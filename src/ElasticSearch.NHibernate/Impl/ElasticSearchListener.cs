#region license
// Copyright (c) 2007-2010 Mauricio Scheffer
// Copyright (c) 2011 Ufuk Kayserilioglu - PhonoClick
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
using ElasticSearch.Client;
using ElasticSearch.NHibernate.Engine;
using NHibernate;
using NHibernate.Event;
using NHibernate.Util;
using log4net;

namespace ElasticSearch.NHibernate.Impl {
  public class ElasticSearchListener : IAutoFlushEventListener, IFlushEventListener, IPostInsertEventListener, IPostDeleteEventListener, IPostUpdateEventListener {
    private static readonly ILog Logger = LogManager.GetLogger(typeof (ElasticSearchListener));

    private readonly WeakHashtable entitiesToAdd = new WeakHashtable();
    private readonly WeakHashtable entitiesToDelete = new WeakHashtable();

    internal SearchContext SearchContext { get; set; }

    private ElasticClient Client {
      get { return SearchContext.Client; }
    }

    public bool Commit { get; set; }

    private void Add(ITransaction s, object entity) {
      AddToHashtable(entitiesToAdd, s, entity);
    }

    private void Delete(ITransaction s, object entity) {
      AddToHashtable(entitiesToDelete, s, entity);
    }

    private void AddToHashtable(WeakHashtable table, ITransaction s, object entity) {
      lock (table.SyncRoot) {
        if (!table.Contains(s)) {
          table.Add(s, new ArrayList());
        }
        var list = table[s] as IList;
        if (list != null && !list.Contains(entity)) {
          list.Add(entity);
        }
      }
    }

    public virtual void OnPostInsert(PostInsertEvent e) {
      UpdateInternal(e);
    }

    public virtual void OnPostUpdate(PostUpdateEvent e) {
      UpdateInternal(e);
    }

    private readonly List<FlushMode> deferFlushModes = new List<FlushMode> {
            FlushMode.Commit, 
            FlushMode.Never,
        };

    public bool DeferAction(IEventSource e) {
      if (e.TransactionInProgress)
        return true;
      var s = (ISession)e;
      return deferFlushModes.Contains(s.FlushMode);
    }

    public void UpdateInternal(AbstractPostDatabaseOperationEvent e) {
      if (e.Entity == null)
        return;

      var isIndexed = SearchContext.IsIndexed(e.Entity.GetType());
      if (!isIndexed)
        return;

      if (DeferAction(e.Session))
        Add(e.Session.Transaction, e);
      else
      {
        var builder = SearchContext.GetBuilder(e.Entity);
        DoAdd(e, builder);
        if (Commit)
          Client.Refresh();
      }
    }

    public virtual void OnPostDelete(PostDeleteEvent e) {
      if (e.Entity == null)
        return;

      var builder = SearchContext.GetBuilder(e.Entity);
      if (builder == null)
        return;

      if (DeferAction(e.Session))
        Delete(e.Session.Transaction, e);
      else {
        DoDelete(e, builder);
        if (Commit)
          Client.Refresh();
      }
    }

    private void DoAdd(IPostDatabaseOperationEventArgs e, DocumentBuilder builder) {
      if (builder == null)
        builder = SearchContext.GetBuilder(e.Entity);
      if (builder == null)
        return;

      var doc = e is PostInsertEvent ? null :
                                              Client.Get<Dictionary<string, object>>(
                                                Client.Settings.DefaultIndex,
                                                builder.GetTypeName(),
                                                builder.GetIdFromEntity(e.Entity));
      Client.Index(
        builder.GetDocumentFromEntity(SearchContext, e.Entity, doc),
        Client.Settings.DefaultIndex,
        builder.GetTypeName(),
        builder.GetIdFromEntity(e.Entity));
    }

    private void DoDelete(IPostDatabaseOperationEventArgs e, DocumentBuilder builder) {
      if (builder == null)
        builder = SearchContext.GetBuilder(e.Entity);
      if (builder == null)
        return;

      Client.Delete(Client.Settings.DefaultIndex, builder.GetTypeName(), builder.GetIdFromEntity(e.Entity));
    }

    public bool DoWithEntities(WeakHashtable entities, ITransaction s, Action<AbstractPostDatabaseOperationEvent> action) {
      lock (entities.SyncRoot) {
        try {
          var hasToDo = entities.Contains(s);
          if (hasToDo)
            foreach (var i in (IList)entities[s])
              action((AbstractPostDatabaseOperationEvent)i);
          entities.Remove(s);
          return hasToDo;
        }
        catch (Exception ex) {
          Logger.ErrorFormat("Unhandled exception in DoWithEntities({0}, {1}, {2}). Exception = {3}", entities, s, action, ex);
          return false;
        }
      }
    }

    public void OnFlush(FlushEvent e) {
      OnFlushInternal(e);
    }

    public void OnFlushInternal(AbstractEvent e) {
      var added = DoWithEntities(entitiesToAdd, e.Session.Transaction, d => DoAdd(d, null));
      var deleted = DoWithEntities(entitiesToDelete, e.Session.Transaction, d => DoDelete(d, null));
      if (Commit && (added || deleted))
        Client.Refresh();
    }

    public void OnAutoFlush(AutoFlushEvent e) {
      OnFlushInternal(e);
    }
  }
}