using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ElasticSearch.NHibernate.Attributes {
  [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
  public class IdFieldAttribute : Attribute {
  }
}
