using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ElasticSearch.Client;
using ElasticSearch.Client.DSL;
using ElasticSearch.NHibernate.Attributes;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using FluentNHibernate.Mapping;
using NHibernate;
using NHibernate.Event;
using NHibernate.Linq;
using NHibernate.Tool.hbm2ddl;

namespace ElasticSearch.NHibernate.Test {
  /*
 * Entities
 */

  public class Entity {
    [IdField]
    public virtual int Id { get; set; }
  }

  [SubIndexed]
  public class NestedProperty : Entity
  {
    [Field]
    public virtual string Prop1 { get; set; }
    public virtual string Prop2 { get; set; }
  }

  [SubIndexed]
  public class NestedCollectionItem : Entity
  {
    [Field]
    public virtual string Prop3 { get; set; }
    public virtual string Prop4 { get; set; }
  }

  [Indexed]
  public class Post : Entity {
    public Post() {
      Comments = new List<Comment>();
      NestedList = new List<NestedCollectionItem>();
    }

    [Field]
    public virtual string Author { get; set; }
    [Field]
    public virtual string Title { get; set; }
    [Field(null)]
    public virtual string Body { get; set; }
    public virtual DateTime CreationDate { get; set; }
    public virtual DateTime UpdateDate { get; set; }

    public virtual IList<Comment> Comments { get; set; }

    [Field]
    public virtual NestedProperty NestedProperty { get; set; }

    [Field]
    public virtual IList<NestedCollectionItem> NestedList { get; set; }

    public virtual Post AddComment(Comment comment) {
      Comments.Add(comment);
      comment.Post = this;
      return this;
    }

    public override string ToString() {
      return Title;
    }
  }

  [Indexed]
  public class Comment : Entity {
    public virtual Post Post { get; set; }

    [Field]
    public virtual string Author { get; set; }
    [Field]
    public virtual string Body { get; set; }

    public override string ToString() {
      return Author + " - " + Body;
    }
  }

  /*
   * Fluent Mappings
   */
  public class NestedMap : ClassMap<NestedProperty>
  {
    public NestedMap()
    {
      Id(a => a.Id);
      Map(x => x.Prop1);
      Map(x => x.Prop2);
    }
  }

  public class NestedCollectionItemMap : ClassMap<NestedCollectionItem>
  {
    public NestedCollectionItemMap()
    {
      Id(a => a.Id);
      Map(x => x.Prop3);
      Map(x => x.Prop4);
    }
  }

  public class PostMap : ClassMap<Post> {
    public PostMap() {
      Id(a => a.Id);
      Map(a => a.Author);
      Map(a => a.Title);
      //Map(a => a.Body).Nullable();
      Map(a => a.CreationDate);
      Map(a => a.UpdateDate);
      HasMany(a => a.Comments);
      References(a => a.NestedProperty);
      HasMany(a => a.NestedList).Cascade.AllDeleteOrphan().Inverse();
    }
  }

  public class CommentMap : ClassMap<Comment> {
    public CommentMap() {
      Id(c => c.Id);
      Map(c => c.Author);
      Map(c => c.Body);
      References(c => c.Post);
    }
  }

  class Program {
    static void Main(string[] args) {
      try {
        File.Delete("db.sqlite");
        IConnectionSettings connectionSettings = new ConnectionSettings("demeter", 9200)
          .SetDefaultIndex("nhibernate");

        var factory = Fluently.Configure()
          .Database(SQLiteConfiguration.Standard.UsingFile("db.sqlite"))
          .Mappings(m => m.FluentMappings.AddFromAssemblyOf<Entity>())
          .ExposeConfiguration(c => new SchemaExport(c).Create(false, true))
          .ExposeConfiguration(c => ElasticSearch.Configure(c)
                                      .ConnectionSettings(()=> connectionSettings)
                                      .Mappings()
                                      .BuildConfiguration())
          .BuildSessionFactory();

        var originalPost = PopulateData(factory);



        using (var session = factory.OpenSession())
        {
          var p = session.Get<Post>(originalPost.Id);
          // Update the name of post and null-out the body.
          p.Body = null;
          p.Author = "ufukiko";
          p.Title = "Test title";

          //session.Save(p);
          session.Flush();
        }

        //using (var session = factory.OpenSession()) {
        //  // Test if everything was saved....
        //  foreach (var post in session.Query<Post>()) {
        //    Console.WriteLine("Got Post [{0}] with comment ids ({1})", post, string.Join(", ", post.Comments.Select(c => c.Id.ToString())));
        //  }
        //  foreach (var comment in session.Query<Comment>()) {
        //    Console.WriteLine("Got Comment [{0}] with post id ({1})", comment, comment.Post.Id);
        //  }

        //  // Now test search session.
        //  var searchSession = ElasticSearch.CreateFullTextSession(session);

        //  searchSession.Refresh();

        //  var results = searchSession
        //    .CreateFullTextQuery(new Query(new QueryString("blog")))
        //    .SetFirstResult(0)
        //    .SetMaxResults(10)
        //    .SetHighlightFields("author", "body")
        //    .ToQueryable<object>();

        //  foreach (var result in results) {
        //    if (result.Entity is Comment) {
        //      Console.WriteLine("+-+- Got comment by {0} : {1}", (result.Entity as Comment).Author, (result.Entity as Comment).Body);
        //      Console.WriteLine("     Score: {0}", result.Hit.Score);
        //      Console.WriteLine("     Highlights: {0}", HighlightsToString(result));
        //    }
        //    else if (result.Entity is Post) {
        //      Console.WriteLine("+-+- Got post by {0} : {1}", (result.Entity as Post).Author, (result.Entity as Post).Body);
        //      Console.WriteLine("     Score: {0}", result.Hit.Score);
        //      Console.WriteLine("     Highlights: {0}", HighlightsToString(result));
        //    }
        //  }
        //}
      }
      catch (Exception ex) {
        Console.WriteLine(ex);
      }
      Console.WriteLine("Done.");
      Console.ReadKey();
    }

    private static string HighlightsToString(SearchResult<object> result) {
      return string.Join("," , result.Hit.Highlights.ToDictionary(h => h.Key, h => string.Join(" ", h.Value)));
    }

    private static Post PopulateData(ISessionFactory factory) {
      using (var session = factory.OpenSession()) {
        //using (var tx = session.BeginTransaction()) {
        var fulltextSession = ElasticSearch.CreateFullTextSession(session);
        fulltextSession.PurgeAll();

        var post = new Post {
                              Author = "ufuk",
                              Title = "Test Post",
                              Body = "This is a test blog post...",
                              CreationDate = DateTime.Now.AddDays(-1.0),
                              UpdateDate = DateTime.Now,
                              NestedProperty = new NestedProperty {Prop1 = "prop1", Prop2 = "prop2"}
                            }
          .AddComment(new Comment {
                                    Author = "Anonymous Coward 1",
                                    Body = "Your blog sucks",
                                  })
          .AddComment(new Comment {
                                    Author = "Anonymous Coward 2",
                                    Body = "I really like your blog.",
                                  })
          .AddComment(new Comment {
                                    Author = "Anonymous Coward 3",
                                    Body = "You are the best. Keep on rocking!",
                                  })
          .AddComment(new Comment {
                                    Author = "ufuk",
                                    Body = "Thanks guys for all the comments",
                                  });

        post.NestedList.Add(new NestedCollectionItem {Prop3 = "Prop3", Prop4 = "Prop4"});

        foreach (var comment in post.Comments) {
          session.Save(comment);
        }

        session.Save(post.NestedProperty);
        session.Save(post);

        return post;
        //tx.Commit();
      }
    }
  }
}
