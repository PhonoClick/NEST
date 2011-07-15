using System;

namespace ElasticSearch.NHibernate.Attributes {
  [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true)]
  public class FieldAttribute : Attribute {
    private object skipValue;
    private bool hasSkipValue = false;

    public FieldAttribute() {      
    }

    public FieldAttribute(object skipValue) {
      this.skipValue = skipValue;
      this.hasSkipValue = true;
    }

    public bool ShouldSkipValue(object value) {
      return (hasSkipValue && value == skipValue);
    }
  }
}