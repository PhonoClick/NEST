using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ElasticSearch.Client.DSL {
  public class Highlight {
    public Dictionary<string, Field> Fields { get; private set; }

    public Highlight() {
      Fields = new Dictionary<string, Field>();
    }

    public Highlight(params string[] fields) : this() {
      fields.ForEachWithIndex((f, i) => Fields.Add(f, new Field()));
    }
  }
}
