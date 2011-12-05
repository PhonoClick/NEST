#region license
// Copyright (c) 2007-2010 Mauricio Scheffer
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
//  
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ElasticSearch.Client;
using ElasticSearch.Client.Thrift;
using ElasticSearch.NHibernate.Engine;
using NHibernate;
using NHibernate.Criterion;
using NHibernate.Engine;
using NHibernate.Engine.Query;
using NHibernate.Impl;
using ElasticSearch.Client.DSL;
using NHibernate.Proxy;
using NHibernate.Util;
using IQuery = NHibernate.IQuery;

namespace ElasticSearch.NHibernate.Impl {
  public class ElasticSearchQueryImpl : AbstractQueryImpl, IElasticSearchQuery {
    private readonly Query esQuery;
    //private readonly ISession session;
    private SearchContext searchContext;
    private int maxResultNumber = -1;
    private int firstResultOffset = -1;
    private string[] highlightFields;

    private SearchContext SearchContext {
      get { return searchContext ?? (searchContext = SearchContext.GetInstance(Session)); }
    }

    private ElasticClient Client {
      get { return SearchContext.Client; }
    }

    private ISession CurrentSession {
      get { return (ISession)Session; }
    }

    public ElasticSearchQueryImpl(Query esQuery, FlushMode flushMode, ISession session, ParameterMetadata parameterMetadata, params Type[] types) :
      base(esQuery.ToString(), flushMode, session.GetSessionImplementation(), parameterMetadata) {
      this.esQuery = esQuery;
    }

    /// <summary>
    /// Ignored
    /// </summary>
    /// <param name="alias"></param>
    /// <param name="lockMode"></param>
    /// <returns>this</returns>
    public override IQuery SetLockMode(string alias, LockMode lockMode) {
      return this;
    }

    public override int ExecuteUpdate() {
      throw new HibernateException("Operation not supported. For Solr updates use the SolrNet interfaces");
    }

    public new IElasticSearchQuery SetMaxResults(int maxResults) {
      this.maxResultNumber = maxResults;
      return this;
    }

    public new IElasticSearchQuery SetFirstResult(int firstResult) {
      this.firstResultOffset = firstResult;
      return this;
    }

    public IElasticSearchQuery SetHighlightFields(params string[] fields) {
      this.highlightFields = fields;
      return this;
    }

    public IElasticSearchQuery SetHighlightFields<T>() {
      var builder = SearchContext.GetBuilderByType(typeof (T));
      if (builder == null) {
        throw new ArgumentException(string.Format("Cannot search for type {0} that is not mapped for search.", typeof (T)));
      }

      return SetHighlightFields(builder.GetFieldNames());
    }

    public IQueryable<SearchResult<T>> ToQueryable<T>() {
      var result = Execute<T>();

      if (!result.IsValid)
        throw new HibernateException("Failed to fetch any results from Elastic Search server.",
          result.ConnectionError.OriginalException);

      return result.HitsMetaData
        .Hits
        .Select(hit => {
                  var entity = default(T);
                  var entityType = GetEntityTypeFromName((string) hit.SourceDictionary[DocumentBuilder.CLASS_FIELDNAME]);
                  var builder = SearchContext.GetBuilderByType(entityType);
                  if (builder != null)
                    entity = (T) LoadObject(entityType, builder.GetIdFromStringId(hit.Id));
                  return new SearchResult<T>(hit, entity);
                })
        .AsQueryable();
    }

    public int Scalar<T>(string query) where T : class
    {
      var result = ExecuteScalar<T>(query);

      return result.Count;
    }

    private CountResponse ExecuteScalar<T>(string query) where T : class
    {
      return Client.Count<T>(query);
    }

    public override IEnumerable Enumerable() {
      return Enumerable<object>();
    }

    private static Type GetEntityTypeFromName(string typeName) {
      return ReflectHelper.ClassForName(typeName);      
    }
    
    private object LoadObject(Type type, object id) {
      object maybeProxy = CurrentSession.Load(type, id);

      // TODO: Initialize call and error trapping
      try {
        NHibernateUtil.Initialize(maybeProxy);
      }
      catch (Exception e) {
        if (e.GetType().IsAssignableFrom(typeof(ObjectNotFoundException))) {
          //log.Debug("Object found in Search index but not in database: "
          //          + entityInfo.Clazz + " wih id " + entityInfo.Id);
          maybeProxy = null;
        }
        else
          throw;
      }

      return maybeProxy;
    }

    /// <summary>
    /// Return the query results as an <see cref="System.Collections.Generic.IEnumerable{T}"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    /// <remarks>This is not a lazy IEnumerable</remarks>
    public override IEnumerable<T> Enumerable<T>() {
      var result = Execute<T>();

      if (!result.IsValid)
        throw new HibernateException("Failed to fetch any results from Elastic Search server.",
          result.ConnectionError.OriginalException);

      return result.HitsMetaData
        .Hits
        .Select(hit => {
                  var entityType = GetEntityTypeFromName(hit.Type);
                  var builder = SearchContext.GetBuilderByType(entityType);
                  if (builder == null)
                    return default(T);
                  return (T) LoadObject(entityType, builder.GetIdFromStringId(hit.Id));
                });
    }

    public override IList List() {
      var arrayList = new ArrayList();
      List(arrayList);
      return arrayList;
    }

    public override void List(IList results) {
      var r = Enumerable<object>();
      foreach (var result in r) {
        results.Add(result);
      }
    }

    public override IList<T> List<T>() {
      var arrayList = new ArrayList();
      List(arrayList);
      return (T[])arrayList.ToArray(typeof(T));
    }

    /// <summary>
    /// Null
    /// </summary>
    protected override IDictionary<string, LockMode> LockModes {
      get { return null; }
    }

    private QueryResponse Execute<T>() {
      var search = new Search() {
        Query = esQuery,
      };
      if (firstResultOffset != -1) {
        search = search.Skip(firstResultOffset);
      }
      if (maxResultNumber != -1) {
        search = search.Take(maxResultNumber);
      }
      if (highlightFields != null) {
        search = search.HighlightOnFields(highlightFields);
      }
      
      var builder = SearchContext.GetBuilderByType(typeof(T));
      
      return builder == null 
              ? Client.Search(search) 
              : Client.Search(search, builder.GetTypeName());
    }
  }
}