using System;
using System.Configuration;
using System.Linq;
using ElasticSearch.Client.DSL;
using Newtonsoft.Json;
using ElasticSearch.Client.Thrift;

namespace ElasticSearch.Client
{
  public partial class ElasticClient
  {
    public CountResponse Count(Search search, params string[] typeNames)
    {
      var version = ConfigurationManager.AppSettings.Get("ElasticSearch.Version");
      if (string.IsNullOrEmpty(version) || version=="old")
      {
        var index = this.Settings.DefaultIndex;
        index.ThrowIfNullOrEmpty("Cannot infer default index for current connection.");

        var typeName = string.Join(",", typeNames);
        if (string.IsNullOrEmpty(typeName))
        {
          typeName = "*";
        }
        var single = search.Query.Queries.FirstOrDefault();
        var rawQuery = this.SerializeCommand(single.Value);
        var firstletterLCaseKey = Char.ToLowerInvariant(single.Key[0]) + single.Key.Substring(1);
        rawQuery = "{\"" + firstletterLCaseKey + "\": " + rawQuery + "}";
        return Count(rawQuery, index, typeName);
      }
      else
      {
        var rawQuery = this.SerializeCommand(search);
        return this.NewCount(rawQuery, typeNames);        
      }
    }

    public CountResponse Count(string query, string index, string typeName)
    {
      query.ThrowIfNull("query");
      index.ThrowIfNull("index");
      string path = null;
      if (typeName.IsNullOrEmpty())
        path = this.createPath(index) + "_count";
      else
        path = this.createPath(index, typeName) + "_count";
      if (!query.StartsWith("{"))
        query = " { " + query + " }";
      return _Count(path, query);
    }

    public CountResponse NewCount(string query, params string[] typeNames)
    {
      var index = this.Settings.DefaultIndex;
      index.ThrowIfNullOrEmpty("Cannot infer default index for current connection.");

      var typeName = string.Join(",", typeNames);
      if (string.IsNullOrEmpty(typeName))
      {
        typeName = "*";
      }
      string path = this.createPath(index, typeName) + "_count";
      var status = this.Connection.PostSync(path, query);
      if (status.Error != null)
      {
        return new CountResponse()
        {
          IsValid = false,
          ConnectionError = status.Error
        };
      }

      var response = JsonConvert.DeserializeObject<CountResponse>(status.Result);

      return response;
    }

    private CountResponse _Count(string path, string query)
    {
      var status = this.Connection.PostSync(path, query);
      if (status.Error != null)
      {
        return new CountResponse()
        {
          IsValid = false,
          ConnectionError = status.Error
        };
      }

      var response = JsonConvert.DeserializeObject<CountResponse>(status.Result, this.SerializationSettings);
      return response;
    }

  }
}
