namespace ElasticSearch.Client
{

	public partial class ElasticClient
	{
		public bool PutAlias(string index,string alias)
		{
			index.ThrowIfNullOrEmpty("Cannot infer default index for current connection.");
      var result = this.Connection.DoSync("PUT",index+"/_alias/"+alias, "{\"routing\":\""+alias+"\",\"filter\":{\"term\":{\"domain.name\":\""+alias+"\"}}}");
      Logger.DebugFormat("alias created on: {0} is: {1} result is {2}", index, alias, result.Success);
		  return result.Success;
		}
	}
}
