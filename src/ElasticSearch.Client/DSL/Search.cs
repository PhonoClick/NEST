using System;
using System.ComponentModel;
using Newtonsoft.Json;

namespace ElasticSearch.Client.DSL
{
	public class Search
	{
		public Query Query { get; set; }

		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
		[DefaultValue(null)]
		public Highlight Highlight { get; set; }
		//public List<Facet> Facets { get; set; }

		[JsonProperty(PropertyName = "from", DefaultValueHandling = DefaultValueHandling.Ignore)]
		[DefaultValue(-1)]
		public int Skipping { get; private set; }

		[JsonProperty(PropertyName = "size", DefaultValueHandling = DefaultValueHandling.Ignore)]
		[DefaultValue(-1)]
		public int Taking { get; private set; }
		
		public int Explain { get; set; }
		
		public Search () {
			Skipping = -1;
			Taking = -1;
		}
		
		public Search Skip(int skip)
		{
			this.Skipping  = skip;
			return this;
		}
		public Search Take(int take)
		{
			this.Taking = take;
			return this;
		}
		
		public Search HighlightOnFields(params string[] fields) {
			this.Highlight = new Highlight(fields);
			return this;
		}
	}
}

