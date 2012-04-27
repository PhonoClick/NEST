using System;

namespace ElasticSearch.NHibernate.Attributes
{
  [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
  public class SubIndexedAttribute : Attribute { }
}