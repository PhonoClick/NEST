using System;
using Fasterflect;
using System.Text.RegularExpressions;
using System.Globalization;

namespace ElasticSearch.Client
{

	public partial class ElasticClient
	{
		private Regex _bulkReplace = new Regex(@",\n|^\[", RegexOptions.Compiled | RegexOptions.Multiline);

		private Func<T, string> CreateIdSelector<T>() where T : class
		{
			Func<T, string> idSelector = null;
			var type = typeof(T);
			var idProperty = type.GetProperty("Id");
			if (idProperty != null)
			{
				if (idProperty.PropertyType == typeof(int))
					idSelector = (@object) => ((int)@object.TryGetPropertyValue("Id")).ToString();
				else if (idProperty.PropertyType == typeof(int?))
					idSelector = (@object) =>
					{
						int? val = (int?)@object.TryGetPropertyValue("Id");
						return (val.HasValue) ? val.Value.ToString() : string.Empty;
					};
				else if (idProperty.PropertyType == typeof(string))
					idSelector = (@object) => (string)@object.TryGetPropertyValue("Id");
				else
					idSelector = (@object) => (string)Convert.ChangeType(@object.TryGetPropertyValue("Id"), typeof(string), CultureInfo.InvariantCulture);
			}
			return idSelector;
		}

    private string CreatePathFor(object @object) {
      var index = this.Settings.DefaultIndex;
      if (string.IsNullOrEmpty(index))
        throw new NullReferenceException("Cannot infer default index for current connection.");
      return this.CreatePathFor(@object, index, InferTypeName(@object));
    }

		private string CreatePathFor<T>(T @object) {
			var index = this.Settings.DefaultIndex;
			if (string.IsNullOrEmpty(index))
				throw new NullReferenceException("Cannot infer default index for current connection.");
			return this.CreatePathFor<T>(@object, index);
		}

		private string CreatePathFor<T>(T @object, string index) {
			var typeName = this.InferTypeName<T>();
			return this.CreatePathFor(@object, index, typeName);			
		}

		private string CreatePathFor(object @object, string index, string type)
		{
			@object.ThrowIfNull("object");
			index.ThrowIfNull("index");
			type.ThrowIfNull("type");

			var path = this.createPath(index, type);

			var id = this.GetIdFor(@object);
			if (!string.IsNullOrEmpty(id))
				path = this.createPath(index, type, id);

			return path;

		}
		private string CreatePathFor(object @object, string index, string type, object id)
		{
			@object.ThrowIfNull("object");
			index.ThrowIfNull("index");
			type.ThrowIfNull("type");

			return this.createPath(index, type, id);
		}

		private string GetIdFor(object @object)
		{
			var type = @object.GetType();
			var idProperty = type.GetProperty("Id");
			int? id = null;
			string idString = string.Empty;
			if (idProperty != null)
			{
				if (idProperty.PropertyType == typeof(int)
					|| idProperty.PropertyType == typeof(int?))
					id = (int?)@object.TryGetPropertyValue("Id");
				if (idProperty.PropertyType == typeof(string))
					idString = (string)@object.TryGetPropertyValue("Id");
				if (id.HasValue)
					idString = id.Value.ToString();
			}
			return idString;
		}

    private string InferTypeName(object o) {
      var type = o.GetType();
      var typeName = type.Name;
      if (this.Settings.TypeNameInferrer != null)
        typeName = this.Settings.TypeNameInferrer(type);
      if (this.Settings.TypeNameInferrer == null || string.IsNullOrEmpty(typeName))
        typeName = Inflector.MakePlural(type.Name).ToLower();
      return typeName;
    }

		private string InferTypeName<T>() {
			var type = typeof(T);
			var typeName = type.Name;
			if (this.Settings.TypeNameInferrer != null)
				typeName = this.Settings.TypeNameInferrer(type);
			if (this.Settings.TypeNameInferrer == null || string.IsNullOrEmpty(typeName))
				typeName = Inflector.MakePlural(type.Name).ToLower();
			return typeName;
		}

		private string createPath(string index, string type)
		{
			return "{0}/{1}/".F(index, type);
		}
		private string createPath(string index, string type, object id)
		{
			return "{0}/{1}/{2}".F(index, type, id);
		}

    private string createPath(string index) {
      return "{0}/".F(index);
    }

	  public void Refresh() {
      this.Connection.PostSync("_refresh", "");
	  }

	}
}
