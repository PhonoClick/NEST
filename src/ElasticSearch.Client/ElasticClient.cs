using System;
using System.Collections.Generic;
using System.Globalization;
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
using log4net;

namespace ElasticSearch.Client
{

	public partial class ElasticClient {
	  private static readonly ILog Logger = LogManager.GetLogger(typeof (ElasticClient));
		private IConnection Connection { get; set; }
		private bool _gotNodeInfo = false;
	  private bool _IsValid { get; set; }
		private ElasticSearchVersionInfo _VersionInfo { get; set; }
		private JsonSerializerSettings SerializationSettings { get; set; }
    private JsonSerializerSettings CommandSerializationSettings { get; set; }
    private PropertyNameResolver PropertyNameResolver { get; set; }

    public IConnectionSettings Settings { get; private set; }

		public bool IsValid
		{
			get
			{
				if (!this._gotNodeInfo)
					this.GetNodeInfo();
				return this._IsValid;
			}
		}
	
		public ElasticSearchVersionInfo VersionInfo
		{
			get
			{
				if (!this._gotNodeInfo)
					this.GetNodeInfo();
				return this._VersionInfo;
			}
		}

		public bool TryConnect(out ConnectionStatus status)
		{
			try
			{
				status = this.GetNodeInfo();
				return this.IsValid;
			}
			catch (Exception e)
			{
				status = new ConnectionStatus(e);
			}
			return false;
		}
		
		public ElasticClient(IConnectionSettings settings): this(settings, false)
		{
		}

		public ElasticClient(IConnectionSettings settings,bool  useThrift)
		{
			this.Settings = settings;
      this.Connection = useThrift ? (IConnection) new ThriftConnection(settings) : new Connection(settings);
			this.SerializationSettings = new JsonSerializerSettings()
			{
				ContractResolver = new CamelCasePropertyNamesContractResolver(),
				NullValueHandling = NullValueHandling.Ignore,
				Converters = new List<JsonConverter> { new IsoDateTimeConverter(), new QueryJsonConverter() },
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore
			};
     
      this.CommandSerializationSettings = this.SerializationSettings;

			this.PropertyNameResolver = new PropertyNameResolver(this.SerializationSettings);
		}

    public static string ToCamelCase(string s) {
      if (string.IsNullOrEmpty(s) || !char.IsUpper(s[0]))
        return s;
      string str = char.ToLower(s[0], CultureInfo.InvariantCulture).ToString((IFormatProvider)CultureInfo.InvariantCulture);
      if (s.Length > 1)
        str = str + s.Substring(1);
      return str;
    }

    public string ResolvePropertyName(string propertyName) {
      return ToCamelCase(propertyName);
    }

    public string SerializeObject(object @object) {
      var result = JsonConvert.SerializeObject(@object, Formatting.None, SerializationSettings);
      Logger.DebugFormat("Serialized object: {0}", result);
      return result;
    }

		public string SerializeCommand(object @object) {
		  var result = JsonConvert.SerializeObject(@object, Formatting.None, this.CommandSerializationSettings);
      Logger.DebugFormat("Serialized command: {0}", result);
		  return result;
		}

	  private ConnectionStatus GetNodeInfo()
		{
			var response = this.Connection.GetSync("");
      Logger.DebugFormat("Got node info: {0}", response);
      if (response != null && response.Success)
			{
				JObject o = JObject.Parse(response.Result);
				if (o["ok"] == null)
				{
					this._IsValid = false;
					return response;
				}
				
				this._IsValid = (bool)o["ok"];
				
				JObject version = o["version"] as JObject;
			  if (version != null)
			    this._VersionInfo = JsonConvert.DeserializeObject<ElasticSearchVersionInfo>(version.ToString());

			  this._gotNodeInfo = true;
			}
			return response;
		}
		
		
	

	
	}

  //public class IndexedObjectContractResolver : DefaultContractResolver {
  //  //protected override List<MemberInfo> GetSerializableMembers(Type objectType) {
  //  //  var members = base.GetSerializableMembers(objectType);
  //  //  return members.Where(m => m.HasAttribute(typeof (FieldAttribute))).ToList();
  //  //}
  //  protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization) {
  //    var properties = base.CreateProperties(type, memberSerialization);

  //    properties = properties.ToList();
  //    properties.Add(new JsonProperty() {
  //                                        PropertyName = "_type",
  //                                        ValueProvider = new StaticValueProvider(type.FullName + ", " + type.Assembly.GetName().Name),
  //                                        Ignored = false,
  //                                        Readable = true,
  //                                        Writable = false
  //                                      });

  //    return properties;
  //  }

  //  public class StaticValueProvider : IValueProvider {
  //    private object value;

  //    public StaticValueProvider(object value) {
  //      this.value = value;
  //    }

  //    public void SetValue(object target, object value) {
  //      return;
  //    }

  //    public object GetValue(object target) {
  //      return value;
  //    }
  //  }
  //}

}
