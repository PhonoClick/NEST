using System.Collections.Generic;
using Newtonsoft.Json;

namespace ElasticSearch.Client.Facets
{
    public interface IFacet
    {
    }

    [JsonObject]
    public interface IFacet<T> : IFacet where T : FacetItem
    {
        IEnumerable<T> Items { get; }
    }
    [JsonObject]
    public abstract class Facet : IFacet
    {
    }
}