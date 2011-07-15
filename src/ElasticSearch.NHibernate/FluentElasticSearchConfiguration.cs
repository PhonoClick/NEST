using System.Linq;
using ElasticSearch.Client;
using ElasticSearch.NHibernate.Impl;
using NHibernate.Cfg;

namespace ElasticSearch.NHibernate {
  public class FluentElasticSearchConfiguration {
    private readonly Configuration cfg;
    private IConnectionSettings connectionSettings;

    protected internal FluentElasticSearchConfiguration(Configuration cfg) {
      this.cfg = cfg;
    }

    public FluentElasticSearchConfiguration ConnectionSettings(IConnectionSettings settings) {
      this.connectionSettings = settings;
      return this;
    }

    public FluentElasticSearchConfiguration Mappings() {
      // No need to do anything here. This means map all...
      return this;
    }

    public Configuration BuildConfiguration() {
      var searchContext = new SearchContext(connectionSettings);

      foreach (var type in cfg.ClassMappings.Select(classMapping => classMapping.MappedClass)) {
        searchContext.AddMappedClass(type);
      }

      var listener = new ElasticSearchListener();
      NHHelper.SetListener(cfg, listener);
      listener.SearchContext = searchContext;

      return cfg;
    }
  }
}