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
using ElasticSearch.Client;
using ElasticSearch.Client.DSL;
using ElasticSearch.NHibernate.Impl;
using NHibernate;

namespace ElasticSearch.NHibernate {
    internal class ElasticSearchSession : DelegatingSession, IElasticSearchSession {
      private readonly ISession session;
      private SearchContext searchContext;
      private SearchContext SearchContext {
        get { return searchContext ?? (searchContext = SearchContext.GetInstance(session.GetSessionImplementation())); }
      }

      internal ElasticSearchSession(ISession session) : base(session) {
        if (session == null)
          throw new ArgumentNullException("session");

        this.session = session;
      }

      public QueryResponse RawQuery(string query, params string[] typeNames)
      {
        return SearchContext.Client.Query(query, typeNames);
      }

      public IElasticSearchQuery CreateFullTextQuery(Query query) {
        return new ElasticSearchQueryImpl(query, session.FlushMode, session, null);
      }

      public void Index(object o) {
        var builder = SearchContext.GetBuilder(o);
        if (builder == null)
          return;

        SearchContext.Client.Index(builder.GetDocumentFromEntity(session, searchContext, o, null),
          SearchContext.Client.Settings.DefaultIndex,
          builder.GetTypeName(),
          builder.GetIdFromEntity(o));
      }

      public bool PurgeAll(Type type) {
        var builder = SearchContext.GetBuilderByType(type);
        if (builder == null)
          return false;

        var result = SearchContext.Client.DeleteMapping(builder.GetTypeName());
        return result != null && result.Success;
      }

      public bool PurgeAll() {
        var result = SearchContext.Client.DeleteIndex();
        return result != null && result.Success;
      }

      public bool PurgeAll(string domainName)
      {
        var result = SearchContext.Client.DeleteIndex(domainName);
        return result != null && result.Success;
      }

      public bool CreateAlias(string indexName,string alias)
      {
        return SearchContext.Client.PutAlias(indexName,alias);
      }

      public bool CreateIndex(string indexName)
      {
        var result = SearchContext.Client.CreateIndex(indexName);
        return result != null && result.Success;
      }

      public bool DeleteAllDocs(string indexName)
      {
        return SearchContext.Client.DeleteAllDocuments(indexName);
      }

      public bool DeleteMultiple(string indexName, string data)
      {
        return SearchContext.Client.DeleteMultiple(indexName, data);
      }

      public void Refresh() {
        SearchContext.Client.Refresh();
      }


    }
}