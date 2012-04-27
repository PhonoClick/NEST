using System;
using System.Linq;
using ElasticSearch.Client;
using ElasticSearch.NHibernate.Impl;
using NHibernate.Cfg;

namespace ElasticSearch.NHibernate {
  public class FluentElasticSearchConfiguration {
    private readonly Configuration cfg;
    private Func<IConnectionSettings> connectionSettingsGetter;

    protected internal FluentElasticSearchConfiguration(Configuration cfg) {
      this.cfg = cfg;
    }

    public FluentElasticSearchConfiguration ConnectionSettings(Func<IConnectionSettings> settingsGetter) {
      this.connectionSettingsGetter = settingsGetter;
      return this;
    }

    public FluentElasticSearchConfiguration Mappings() {
      // No need to do anything here. This means map all...
      return this;
    }

    public Configuration BuildConfiguration() {
      var searchContext = new SearchContext(connectionSettingsGetter);

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