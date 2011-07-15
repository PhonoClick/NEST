using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace ElasticSearch.Client.DSL
{
	public class Field
	{
    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    [DefaultValue(default(string))]
		public string Name { get; private set; }
    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    [DefaultValue(default(string))]
    public string Value { get; private set; }
    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    [DefaultValue(null)]
    public double? Boost { get; private set; }

	  public Field() {}
	  public Field(string name, double boost) : this(name, boost, null) {}
		public Field(string name, string value) : this(name, null, value) {}
		public Field (string name, double? boost, string value)
		{
			this.Value = value;
			this.Name = name;
			this.Boost = boost;
		}
	}
}
