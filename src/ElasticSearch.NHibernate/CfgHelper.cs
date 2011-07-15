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
using ElasticSearch.NHibernate.Impl;
using NHibernate.Cfg;

namespace NHibernate.SolrNet {
    /// <summary>
    /// Helper class to configure NHibernate-SolrNet integration.
    /// </summary>
    public class CfgHelper {
        private readonly IConnectionSettings connectionSettings;

      /// <summary>
      /// Gets SolrNet components from a <see cref="IServiceProvider"/>, except for the <see cref="IReadOnlyMappingManager"/>
      /// </summary>
      /// <param name="connectionSettings">Use to specify the connection settings for the ElasticServer service.</param>
      public CfgHelper(IConnectionSettings connectionSettings) {
          this.connectionSettings = connectionSettings;
        }

        /// <summary>
        /// Registers SolrNet's NHibernate listeners
        /// </summary>
        /// <param name="config">NHibernate configuration</param>
        /// <param name="autoCommit">if set to <c>true</c> [auto commit].</param>
        /// <param name="parameters">The add parameters to use when adding a document to the index.</param>
        /// <returns></returns>
        public Configuration Configure(Configuration config, bool autoCommit) {
            foreach (var t in config.ClassMappings) {
                var listenerType = typeof (ElasticSearchListener<>).MakeGenericType(t.MappedClass);
                var listener = (IListenerSettings) Activator.CreateInstance(listenerType, connectionSettings);
                listener.Commit = autoCommit;
                NHHelper.SetListener(config, listener);
            }
            return config;
        }

        /// <summary>
        /// Wraps a NHibernate <see cref="ISession"/> and adds Solr operations
        /// </summary>
        /// <param name="session"><see cref="ISession"/> to wrap</param>
        /// <returns></returns>
        public IElasticSearchSession OpenSession(ISession session) {
            return new ElasticSearchSession(session, connectionSettings);
        }

        /// <summary>
        /// Opens a new NHibernate <see cref="ISession"/> and wraps it to add Solr operations
        /// </summary>
        /// <returns></returns>
        public IElasticSearchSession OpenSession(ISessionFactory sessionFactory) {
            return OpenSession(sessionFactory.OpenSession());
        }

    }
}