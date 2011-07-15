using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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

	public partial class ElasticClient
	{
		public T Get<T>(object id) where T : class
		{
			var index = this.Settings.DefaultIndex;
			index.ThrowIfNullOrEmpty("Cannot infer default index for current connection.");

			var typeName = this.InferTypeName<T>();
		
			return this.Get<T>(id, this.createPath(index, typeName));
		}

		public T Get<T>(string index, string type, object id) where T : class
		{
			return this.Get<T>(id, index + "/" + type + "/");
		}

		private T Get<T>(object id, string path) where T : class
		{
			var response = this.Connection.GetSync(path + id);
			if (response.Error != null && response.Error.HttpStatusCode == HttpStatusCode.NotFound)
				return null;
			var o = JObject.Parse(response.Result);
			var source = o["_source"];
			if (source != null)
			{
				return JsonConvert.DeserializeObject<T>(source.ToString());
			}

			return null;
		}
		
	}
}
