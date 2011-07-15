using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ElasticSearch.Client.DSL
{
	public class Query
	{
	  public Dictionary<string, IQuery> Queries { get; private set; }

		public Query(IQuery query)
		{
      Queries = new Dictionary<string, IQuery>();
		  Queries[query.GetType().Name] = query;
      /*
			if(query is Term)
				this.Term = query as Term;

			else if (query is Fuzzy)
				this.Fuzzy = query as Fuzzy;		
       */ 
		}

	}
}
