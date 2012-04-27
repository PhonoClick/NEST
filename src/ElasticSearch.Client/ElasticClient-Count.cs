using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ElasticSearch.Client.DSL;
using Newtonsoft.Json;
using ElasticSearch.Client.Thrift;

namespace ElasticSearch.Client
{
  public partial class ElasticClient
  {
    //public CountResponse Count()
    //{
    //  return this.Count(@"match_all : { }");
    //}
    //public CountResponse Count<T>() where T : class
    //{
    //  return this.Count<T>(@"match_all : { }");
    //}


    //public CountResponse Count(Search search)
    //{
    //  var rawQuery = this.SerializeCommand(search);
    //  return Count(rawQuery);
    //}


    //public CountResponse Count(string query)
    //{
    //  query.ThrowIfNull("query");
    //  var index = this.Settings.DefaultIndex;
    //  index.ThrowIfNullOrEmpty("Cannot infer default index for current connection.");

    //  return this.Count(query, index);
    //}

    public CountResponse Count(Search search, params string[] typeNames)
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
      return Count(rawQuery,index,typeName);
    }



    //public CountResponse Count(string query, string index)
    //{
    //  query.ThrowIfNull("query");
    //  index.ThrowIfNull("index");
    //  return this.Count(query, index, null);
    //}

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
    //public CountResponse Count<T>(string query) where T : class
    //{
    //  var index = this.Settings.DefaultIndex;
    //  index.ThrowIfNullOrEmpty("Cannot infer default index for current connection.");

    //  var type = typeof(T);
    //  var typeName = this.InferTypeName<T>();
    //  string path = this.createPath(index, type.Name) + "_count";
    //  return _Count(path, query);
    //}
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
