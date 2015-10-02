using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ElasticSearch.Client.Facets;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ElasticSearch.Client
{
  [JsonObject]
  public class QueryResponse {
    public bool IsValid { get; set; }
    public ConnectionError ConnectionError { get; set; }
    public QueryResponse ()
    {
      this.IsValid = true;
      this.Facets = new Dictionary<string, TermFacet>();
    }
    [JsonProperty(PropertyName = "_shards")]
    public ShardsMetaData Shards { get; internal set; }
    [JsonProperty(PropertyName = "hits")]
    public HitsMetaData HitsMetaData { get; internal set; }
    [JsonProperty(PropertyName = "took")]
    public int ElapsedMilliseconds { get; internal set; }

    [JsonProperty(PropertyName = "facets")]
    //[JsonConverter(typeof(DictionaryKeysAreNotPropertyNamesJsonConverter))]
    public IDictionary<string, TermFacet> Facets { get; internal set; }

    public int Total
    {
      get
      {
        if (this.HitsMetaData == null)
          return 0;
        return this.HitsMetaData.Total;
      }
    }
    public float MaxScore
    {
      get
      {
        if (this.HitsMetaData == null)
          return 0;
        return this.HitsMetaData.MaxScore;
      }
    }

    public IEnumerable Documents
    {
      get
      {
        if (this.HitsMetaData != null)
        {
          foreach (var hit in this.HitsMetaData.Hits)
          { 
            yield return hit.Source;
          }
        }
      }
    }
  }
  [JsonObject]
  public class IndicesResponse
  {
    [JsonProperty(PropertyName = "ok")]
    public bool Success { get; private set; }
    public ConnectionStatus ConnectionStatus { get; internal set; }
    [JsonProperty(PropertyName = "_shards")]
    public ShardsMetaData ShardsHit { get; private set; }
  }
  [JsonObject]
  public class ShardsMetaData
  {
    [JsonProperty]
    public int Total { get; internal set; }
    [JsonProperty]
    public int Successful { get; internal set; }
    [JsonProperty]
    public int Failed { get; internal set; }
  }
  [JsonObject]
  public class HitsMetaData {
    [JsonProperty]
    public int Total { get; internal set; }
    [JsonProperty]
    public float MaxScore { get; internal set; }
    [JsonProperty]
    public List<Hit> Hits { get; internal set; }
  }
  [JsonObject]
  public class Hit {
    [JsonProperty(PropertyName = "_source")]
    public object Source { get; internal set; }
    [JsonProperty(PropertyName = "_index")]
    public string Index { get; internal set; }
    [JsonProperty(PropertyName = "_score")]
    public float Score { get; internal set; }
    [JsonProperty(PropertyName = "_type")]
    public string Type { get; internal set; }
    [JsonProperty(PropertyName = "_id")]
    public string Id { get; internal set; }
    [JsonProperty(PropertyName = "highlight")]
    public Dictionary<string, List<string>> Highlights { get; internal set; }

    public IDictionary<string, object> SourceDictionary {
      get {
        return JObject.FromObject(Source)
          .Properties()
          .ToDictionary(
            p => p.Name,
            p => p.Value.ToString() as object
          );
      }
    }
  }
}


