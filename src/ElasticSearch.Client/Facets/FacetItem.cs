using Newtonsoft.Json;

namespace ElasticSearch.Client.Facets
{
	public abstract class FacetItem
	{
		[JsonProperty("count")]
		public virtual long Count { get; internal set; }
	}
}