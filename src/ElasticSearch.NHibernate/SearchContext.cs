using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ElasticSearch.Client;
using ElasticSearch.NHibernate.Attributes;
using ElasticSearch.NHibernate.Engine;
using ElasticSearch.NHibernate.Impl;
using NHibernate;
using NHibernate.Engine;
using NHibernate.Proxy;

namespace ElasticSearch.NHibernate {
  class SearchContext {
    private readonly IConnectionSettings connectionSettings;

    public SearchContext(IConnectionSettings connectionSettings) {
      this.connectionSettings = connectionSettings;
      this.Client = new ElasticClient(connectionSettings);
      DocumentBuilderMap = new Dictionary<Type, DocumentBuilder>();
    }

    public ElasticClient Client { get; private set; }

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
      if (IsIndexed(type)) {
        DocumentBuilderMap.Add(type, new DocumentBuilder(type));
      }
    }

    private static bool IsIndexed(Type type) {
      return type.GetCustomAttributes(typeof(IndexedAttribute), true).Any();
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
