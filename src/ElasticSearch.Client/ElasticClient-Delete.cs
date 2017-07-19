using Newtonsoft.Json.Linq;

namespace ElasticSearch.Client
{
  public partial class ElasticClient {
    public bool Delete(object @object) {
      return Delete(CreatePathFor(@object), InferTypeName(@object));
    }

    public bool DeleteAllDocuments(string indexName)
    {
      indexName.ThrowIfNullOrEmpty("Cannot infer default index for current connection.");
      var result = this.Connection.DoSync("DELETE", indexName + "/_query", "{\"query\":{\"match_all\":{}}}");
      Logger.DebugFormat("alias docs deleted for: {0} result is {1}", indexName, result.Success);
      return result.Success;
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

    public bool DeleteMultiple(string index, string data)
    {
      var response = this.Connection.DeleteMultipleSync(index, data);
      if (response.Error != null)
      {
        return false;
      }
      var o = JObject.Parse(response.Result);
      var ok = o["ok"];
      if (ok != null)
      {
        return (bool)ok;
      }

      return false;
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
