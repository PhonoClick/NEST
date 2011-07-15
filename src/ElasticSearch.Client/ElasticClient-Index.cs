using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace ElasticSearch.Client
{

	public partial class ElasticClient
	{
    public ConnectionStatus Index(object @object) {
      var path = this.CreatePathFor(@object);
      return this._indexToPath(@object, path);
    }

    public ConnectionStatus Index<T>(T @object) {
			var path = this.CreatePathFor<T>(@object);
		    return this._indexToPath(@object, path);
		}

		public ConnectionStatus Index<T>(T @object, string index) where T : class
		{
			var path = this.CreatePathFor<T>(@object, index);
			return this._indexToPath(@object, path);
		}
		public ConnectionStatus Index<T>(T @object, string index, string type) where T : class
		{
			var path = this.CreatePathFor(@object, index, type);
			return this._indexToPath(@object, path);
		}
		public ConnectionStatus Index(object @object, string index, string type, object id)
		{
			var path = this.CreatePathFor(@object, index, type, id);
			return this._indexToPath(@object, path);
		}
		
		private ConnectionStatus _indexToPath(object @object, string path)
		{
			path.ThrowIfNull("path");

		  string json = SerializeObject(@object);

			return this.Connection.PostSync(path, json);
		}

    public void IndexAsync(object @object) {
      var path = this.CreatePathFor(@object);
      this._indexAsyncToPath(@object, path, (s) => { });
    }

		public void IndexAsync<T>(T @object) where T : class
		{
			var path = this.CreatePathFor<T>(@object);
			this._indexAsyncToPath(@object, path, (s)=>{});
		}

		public void IndexAsync<T>(T @object, Action<ConnectionStatus> continuation) where T : class
		{
			var path = this.CreatePathFor<T>(@object);
			this._indexAsyncToPath(@object, path, continuation);
		}
		public void IndexAsync<T>(T @object, string index, Action<ConnectionStatus> continuation) where T : class
		{
			var path = this.CreatePathFor<T>(@object, index);
			this._indexAsyncToPath(@object, path, continuation);
		}
		public void IndexAsync<T>(T @object, string index, string type, Action<ConnectionStatus> continuation) where T : class
		{
			var path = this.CreatePathFor(@object, index, type);
			this._indexAsyncToPath(@object, path, continuation);
		}
		public void IndexAsync(object @object, string index, string type, object id, Action<ConnectionStatus> continuation)
		{
			var path = this.CreatePathFor(@object, index, type, id);
			this._indexAsyncToPath(@object, path, continuation);
		}
		
		private void _indexAsyncToPath(object @object, string path, Action<ConnectionStatus> continuation)
		{
      string json = SerializeObject(@object);
			this.Connection.Post(path, json, continuation);
		}

		public ConnectionStatus Index<T>(IEnumerable<T> objects) where T : class
		{
			var json = this.GenerateBulkCommand(@objects);
			return this.Connection.PostSync("_bulk", json);
		}

		public ConnectionStatus Index<T>(IEnumerable<T> objects, string index) where T : class
		{
			var json = this.GenerateBulkCommand(@objects, index);
			return this.Connection.PostSync("_bulk", json);
		}
		public ConnectionStatus Index<T>(IEnumerable<T> objects, string index, string type) where T : class
		{
			var json = this.GenerateBulkCommand(@objects, index, type);
			return this.Connection.PostSync("_bulk", json);
		}


		public void IndexAsync<T>(IEnumerable<T> objects) where T : class
		{
			var json = this.GenerateBulkCommand(@objects);
			this.Connection.Post("_bulk", json, null);
		}
		public void IndexAsync<T>(IEnumerable<T> objects, Action<ConnectionStatus> continuation) where T : class
		{
			var json = this.GenerateBulkCommand(@objects);
			this.Connection.Post("_bulk", json, continuation);
		}
		public void IndexAsync<T>(IEnumerable<T> objects, string index, Action<ConnectionStatus> continuation) where T : class
		{
			var json = this.GenerateBulkCommand(@objects, index);
			this.Connection.Post("_bulk", json, continuation);
		}
		public void IndexAsync<T>(IEnumerable<T> objects, string index, string type, Action<ConnectionStatus> continuation) where T : class
		{
			var json = this.GenerateBulkCommand(@objects, index, type);
			this.Connection.Post("_bulk", json, continuation);
		}
		
		private string GenerateBulkCommand<T>(IEnumerable<T> objects) where T : class
		{
			objects.ThrowIfNull("objects");

			var index = this.Settings.DefaultIndex;
			if (string.IsNullOrEmpty(index))
				throw new NullReferenceException("Cannot infer default index for current connection.");

			return this.GenerateBulkCommand<T>(objects, index);
		}
		private string GenerateBulkCommand<T>(IEnumerable<T> objects, string index) where T : class
		{
			objects.ThrowIfNull("objects");
			index.ThrowIfNullOrEmpty("index");

			var typeName = this.InferTypeName<T>();

			return this.GenerateBulkCommand<T>(objects, index, typeName);
		}
		private string GenerateBulkCommand<T>(IEnumerable<T> @objects, string index, string typeName) where T : class 
		{
			if (@objects.Count() == 0)
				return null;
			
			var idSelector = this.CreateIdSelector<T>();
			

			var sb = new StringBuilder();
			var command = "{{ \"index\" : {{ \"_index\" : \"{0}\", \"_type\" : \"{1}\", \"_id\" : \"{2}\" }} }}\n";

			//if we can't reflect id let ES create one.
			if (idSelector == null)
				command = "{{ \"index\" : {{ \"_index\" : \"{0}\", \"_type\" : \"{1}\" }} }}\n".F(index, typeName);

			foreach (var @object in objects)
			{
				string jsonCommand = JsonConvert.SerializeObject(@object, Formatting.None, this.SerializationSettings);
				if (idSelector == null)
					sb.Append(command);
				else
					sb.Append(command.F(index, typeName, idSelector(@object)));
				sb.Append(jsonCommand + "\n");
			}
			var json = sb.ToString();
			return json;
		}

	}
}
