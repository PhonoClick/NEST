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

using System.Linq;
using ElasticSearch.Client;
using NHibernate;

namespace ElasticSearch.NHibernate {
    public interface IElasticSearchQuery : IQuery {
        new IElasticSearchQuery SetMaxResults(int maxResults);

        new IElasticSearchQuery SetFirstResult(int firstResult);

        IElasticSearchQuery SetHighlightFields(params string[] fields);

        IElasticSearchQuery SetHighlightFields<T>();

        IQueryable<SearchResult<T>> ToQueryable<T>();
    }

  public class SearchResult<T> {
    public Hit Hit { get; private set; }
    public T Entity { get; private set; }

    internal SearchResult(Hit hit, T entity) {
      Hit = hit;
      Entity = entity;
    }
  }
}