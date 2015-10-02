using System;
using System.Diagnostics;
using System.Linq;
using Newtonsoft.Json;
using ElasticSearch.Client.DSL;

namespace ElasticSearch.Client
{
	public partial class ElasticClient
	{
    public QueryResponse Query(string query, params string[] typeNames) {
      var index = this.Settings.DefaultIndex;
      index.ThrowIfNullOrEmpty("Cannot infer default index for current connection.");

      var typeName = string.Join(",", typeNames);
      if (string.IsNullOrEmpty(typeName)) {
        typeName = "*";
      }
      string path = this.createPath(index, typeName) + "_search";
      var status = this.Connection.PostSync(path, query);
      if (status.Error != null) {
        return new QueryResponse() {
          IsValid = false,
          ConnectionError = status.Error
        };
      }
    
      var response = JsonConvert.DeserializeObject<QueryResponse>(status.Result);

      return response;
    }

		public QueryResponse Query<T>(string query) {
		  return (QueryResponse)Query(query, this.InferTypeName<T>());
      //var index = this.Settings.DefaultIndex;
      //index.ThrowIfNullOrEmpty("Cannot infer default index for current connection.");

      //var type = typeof(T);
      //var typeName = this.InferTypeName<T>();
      //string path = this.createPath(index, typeName) + "_search";

      //var status = this.Connection.PostSync(path, query);
      //if (status.Error != null)
      //{
      //  return new QueryResponse<T>()
      //  {
      //    IsValid = false,
      //    ConnectionError = status.Error
      //  };
      //}

      //var response = JsonConvert.DeserializeObject<QueryResponse<T>>(status.Result);

      //return response;
		}

    public QueryResponse Search(Search search, params string[] typeNames) {
      var sw = Stopwatch.StartNew();       
      var rawQuery = this.SerializeCommand(search);
      Console.WriteLine(rawQuery);
      Logger.DebugFormat("Search with typenames: {0}",rawQuery);
      // time calculation
      var query =  this.Query(rawQuery, typeNames);
      sw.Stop();
      Logger.DebugFormat("Search with typenames - Total time (ms): {0}", sw.ElapsedMilliseconds);
      return query;
    }

		public QueryResponse Search<T>(Search search)
		{
			var rawQuery = this.SerializeCommand(search);
      Console.WriteLine(rawQuery);
      Logger.DebugFormat("Search: {0}", rawQuery);
			return this.Query<T>(rawQuery);
		}

		public QueryResponse Search<T>(string search) {
			return this.Query<T>(search);
		}

		public QueryResponse Search<T>(Query<T> query) where T : class
		{
			var q = query.Queries.First();
			var expression = q.MemberExpression;
			this.PropertyNameResolver.Resolve(expression);
			//var o = this.SerializationSettings.ContractResolver;
			//var contract = this.SerializationSettings.ContractResolver.ResolveContract(expression.Type);
			var search = new Search()
			{

			};

			var rawQuery = this.SerializeCommand(search);
      Logger.DebugFormat("Search with query: {0}", rawQuery);
			return this.Query<T>(rawQuery);
		}

		public QueryResponse Search<T>(IQuery query) where T : class
		{
			return this.Search<T>(new Search()
			{
				Query = new Query(query)

			}.Skip(0).Take(10)
			);
		}
		
	}
}
