using NHibernate;
using NHibernate.Cfg;

namespace ElasticSearch.NHibernate {
  public static class ElasticSearch {
    public static FluentElasticSearchConfiguration Configure() {
      return Configure(new Configuration());
    }

    public static FluentElasticSearchConfiguration Configure(Configuration cfg) {
      return new FluentElasticSearchConfiguration(cfg);
    }

    public static IElasticSearchSession CreateFullTextSession(ISession session) {
      return session as ElasticSearchSession ??
             new ElasticSearchSession(session);
    }
  }
}