using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ElasticSearch.NHibernate.Attributes;
using Fasterflect;

namespace ElasticSearch.NHibernate.Engine {
  internal class DocumentBuilder {
    public const string CLASS_FIELDNAME = "_hibernate_class";

    private readonly Type type;

    private static readonly Type FieldAttributeType = typeof(FieldAttribute);
    private static readonly Type IdFieldAttributeType = typeof(IdFieldAttribute);

    private static readonly Type[] AttributeTypes = new Type[] {
                                                 FieldAttributeType,
                                                 IdFieldAttributeType
                                               };

    private MemberInfo idField;
    private readonly IList<MemberInfo> serializedFields = new List<MemberInfo>();

    public DocumentBuilder(Type type) {
      this.type = type;
      var members = type.MembersAndAttributes(MemberTypes.Property | MemberTypes.Field, AttributeTypes);
      foreach (var member in members) {
        if (member.Value.Any(a => (Type) a.GetType() == IdFieldAttributeType)) {
          if (idField != null)
            throw new InvalidOperationException("Cannot mark more than one field as an IdField.");

          idField = member.Key;
          serializedFields.Add(member.Key);
        }
        else if (member.Value.Any(a => (Type)a.GetType() == FieldAttributeType)) {
          serializedFields.Add(member.Key);
        }
      }
    }

    public Type GetIdFieldType() {
      return idField.Type();
    }

    public object GetIdFromEntity(object entity) {
      return idField.Get(entity);
    }

    public string GetTypeName() {
      return type.Name;
    }

    private string GetClassFieldName() {
      return type.FullName + ", " + type.Assembly.GetName().Name;
    }

    public object GetIdFromStringId(string id) {
      return Convert.ChangeType(id, GetIdFieldType());
    }

    public IDictionary<string, object> GetDocumentFromEntity(SearchContext searchContext, object entity, Dictionary<string, object> doc) {
      var fieldsMap = serializedFields
        .Select(f => {
                  var name = f.Name;
                  var orgValue = f.Get(entity);
                  var value = GetIndexableValue(searchContext, orgValue, doc);
                  var attr = f.Attributes<FieldAttribute>().FirstOrDefault();
                  string docKey = null;
                  if (doc != null)
                    docKey = doc.Keys.SingleOrDefault(k => String.Equals(k, name, StringComparison.InvariantCultureIgnoreCase));
          
                  if (attr != null && attr.ShouldSkipValue(value) && docKey != null) {
                    value = doc[docKey];
                  }

                  return new {
                        Name = name,
                        Value = value
                      };
                })
        .ToDictionary(f => f.Name, f => f.Value);

      // Add the class_fieldname
      fieldsMap.Add(CLASS_FIELDNAME, GetClassFieldName());

      return fieldsMap;
    }

    public string[] GetFieldNames() {
      return serializedFields
        .Where(f => f.Attributes<FieldAttribute>().Count > 0)
        .Select(f => f.Name)
        .ToArray();
    }

    private static object GetIndexableValue(SearchContext searchContext, object value, Dictionary<string, object> doc)
    {
      if (value == null)
      {
        return null;
      }

      var propType = value.GetType();
      var isPrimitive = (propType.IsPrimitive || propType == typeof (string));
      if (isPrimitive)
      {
        return value;
      }

      var builder = searchContext.GetBuilderByType(propType);
      if(builder == null)
      {
        return value;
      }

      var indexableValue = builder.GetDocumentFromEntity(searchContext, value, doc);
      return indexableValue;
    }
  }
}