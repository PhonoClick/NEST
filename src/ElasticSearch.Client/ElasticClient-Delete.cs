using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ElasticSearch.Client.Thrift;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Fasterflect;
using ElasticSearch;
using Newtonsoft.Json.Converters;
using ElasticSearch.Client.DSL;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Reflection;

namespace ElasticSearch.Client
{
  public partial class ElasticClient {
    public bool Delete(object @object) {
      return Delete(CreatePathFor(@object), InferTypeName(@object));
    }

    public bool Delete<T>(T @object) where T : class {
      return Delete(CreatePathFor(@object));
    }
    public bool Delete<T>(int id) where T : class {
      return Delete<T>(id.ToString());
    }

    public bool Delete<T>(string id) where T : class {
      return Delete(id, InferTypeName<T>());
    }

    public bool Delete(string id, string type) {
      var index = this.Settings.DefaultIndex;
      index.ThrowIfNullOrEmpty("Cannot infer default index for current connection.");

      return this.Delete(id, this.createPath(index, type));
    }
    public bool Delete(string index, string type, object id) {
      return this.Delete(createPath(index, type, id));
    }

    private bool Delete(string path) {
      var response = this.Connection.DeleteSync(path);
      if (response.Error != null) {
        return false;
      }
      var o = JObject.Parse(response.Result);
      var ok = o["ok"];
      if (ok != null) {
        return (bool)ok;
      }

      return false;
    }

  }
}
