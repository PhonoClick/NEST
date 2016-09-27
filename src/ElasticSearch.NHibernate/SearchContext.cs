using System;
using System.Collections.Generic;
using System.Linq;
using ElasticSearch.Client;
using ElasticSearch.NHibernate.Attributes;
using ElasticSearch.NHibernate.Engine;
using ElasticSearch.NHibernate.Impl;
using NHibernate.Engine;
using NHibernate.Proxy;

namespace ElasticSearch.NHibernate {
  class SearchContext {
    internal readonly Func<IConnectionSettings> connectionSettingsGetter;

    public SearchContext(Func<IConnectionSettings> connectionSettingsGetter) {
      this.connectionSettingsGetter = connectionSettingsGetter;
      _client = new ElasticClient(connectionSettingsGetter.Invoke());
      DocumentBuilderMap = new Dictionary<Type, DocumentBuilder>();
    }

    private ElasticClient _client;
    public ElasticClient Client { 
      get {
        _client.Settings.DefaultIndex = connectionSettingsGetter.Invoke().DefaultIndex;
        return _client;
      }
      set { _client = value; }
    }

    public Dictionary<Type, DocumentBuilder> DocumentBuilderMap { get; private set; }

    public static SearchContext GetInstance(ISessionImplementor sessionImplementor) {
      SearchContext context = null;

      var listener = (sessionImplementor)
        .Listeners
        .PostInsertEventListeners
        .Where(l => l is ElasticSearchListener)
        .Cast<ElasticSearchListener>()
        .FirstOrDefault();

      if (listener != null)
        context = listener.SearchContext;

      return context;
    }

    public void AddMappedClass(Type type) {
      if (IsIndexed(type) || IsSubIndexed(type))
      {
        DocumentBuilderMap.Add(type, new DocumentBuilder(type));
      }
    }

    public static bool IsIndexed(Type type) {
      return type.GetCustomAttributes(typeof(IndexedAttribute), true).Any();
    }

    private static bool IsSubIndexed(Type type)
    {
      return type.GetCustomAttributes(typeof(SubIndexedAttribute), true).Any();
    }

    public DocumentBuilder GetBuilder(object o) {
      Type type = NHibernateProxyHelper.GuessClass(o);
      return GetBuilderByType(type);
    }

    public DocumentBuilder GetBuilderByType(Type type) {
      return DocumentBuilderMap.ContainsKey(type) ? DocumentBuilderMap[type] : null;
    }
  }
}
