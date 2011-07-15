﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using ElasticSearch.Client;
using HackerNews.Indexer.Domain;
using Nest.TestData;
using Nest.TestData.Domain;

namespace ElasticSearch.Tests
{
	/// <summary>
	///  Tests that test whether the query response can be successfully mapped or not
	/// </summary>
	public class QueryResponseMapperTests : BaseElasticSearchTests
	{
		private string _LookFor = NestTestData.Data.First().Followers.First().FirstName;


		protected void TestDefaultAssertions(QueryResponse queryResponse)
		{
			Assert.True(queryResponse.IsValid);
			Assert.Null(queryResponse.ConnectionError);
			Assert.True(queryResponse.Total > 0, "No hits");
      Assert.True(queryResponse.Documents.Cast<ElasticSearchProject>().Any());
      Assert.True(queryResponse.Documents.Cast<ElasticSearchProject>().Count() > 0);
			Assert.True(queryResponse.Shards.Total > 0);
			Assert.True(queryResponse.Shards.Successful == queryResponse.Shards.Total);
			Assert.True(queryResponse.Shards.Failed == 0);
			Assert.InRange(queryResponse.ElapsedMilliseconds, 0, 200);
				
		}
		[Fact]
		public void BogusQuery()
		{
			var client = this.ConnectedClient;
			QueryResponse<Post> queryResults = client.Search<Post>(
				@"here be dragons"
			);
			Assert.False(queryResults.IsValid);
			Assert.True(queryResults.ConnectionError.HttpStatusCode == System.Net.HttpStatusCode.InternalServerError);
		}
		[Fact]
		public void BoolQuery()
		{
			var queryResults = this.ConnectedClient.Search<ElasticSearchProject>(
				@" { ""query"" : {
						""bool"" : {
							""must"" : {
								""term"" : { ""followers.firstName"" : """ + this._LookFor.ToLower() + @""" }
							},
							""must_not"" : {
								""range"" : {
									""id"" : { ""from"" : 1, ""to"" : 20 }
								}
							},
							""should"" : [
								{
									""term"" : { ""followers.firstName"" : """ + this._LookFor.ToLower() + @""" }
								},
								{
									""term"" : { ""followers.firstName"" : """ + this._LookFor.ToLower() + @""" }
								}
							],
							""minimum_number_should_match"" : 1,
							""boost"" : 1.0
						}	
					} }"
			);
			this.TestDefaultAssertions(queryResults);
			Assert.True(queryResults.Documents.First().Followers
				.Any(f => f.FirstName.Equals(this._LookFor, StringComparison.InvariantCultureIgnoreCase)));
		}

		[Fact]
		public void BoostingQuery()
		{
			var boost2nd = NestTestData.Data.ToList()[2].Followers.First().FirstName;
			var queryResults = this.ConnectedClient.Search<ElasticSearchProject>(
				@" { ""query"" : {
						""boosting"" : {
							""positive"" : {
								""term"" : {
									""followers.firstName"" : """ + boost2nd.ToLower() + @"""
								}
							},
							""negative"" : {
								""term"" : {
									""followers.firstName"" : """ + this._LookFor.ToLower() + @"""
								}
							},
							""negative_boost"" : 0.2
						}
					} }"
			);
			this.TestDefaultAssertions(queryResults);
			Assert.True(queryResults.Documents.First().Followers.First().FirstName != this._LookFor);
		}

		[Fact]
		public void ScoringQuery()
		{
			var queryResults = this.ConnectedClient.Search<ElasticSearchProject>(
				@" { ""query"" : {
						""custom_score"" : {
							""query"" : {
								""term"" : {
									""followers.firstName"" : """ + this._LookFor.ToLower() + @"""
								}
							},
							""script"" : ""_score * 2""
						}
					} }"
			);
			this.TestDefaultAssertions(queryResults);
		}
		[Fact]
		public void ConstantScoreQuery()
		{
			var queryResults = this.ConnectedClient.Search<ElasticSearchProject>(
				@" { ""query"" : {
						""constant_score"" : {
							""filter"" : {
								""term"" : {
									""followers.firstName"" : """ + this._LookFor.ToLower() + @"""
								}
							},
							""boost"" : 1.2
						}
					} }"
			);
			this.TestDefaultAssertions(queryResults);
		}
		[Fact]
		public void DismaxQuery()
		{
			var boost2nd = NestTestData.Data.ToList()[2].Followers.First().FirstName;


			var queryResults = this.ConnectedClient.Search<ElasticSearchProject>(
				@" { ""query"" : {
						""dis_max"" : {
							""tie_breaker"" : 0.7,
							""boost"" : 1.2,
							""queries"" : [
								{
									""term"" : {
										""followers.firstName"" : """ + boost2nd.ToLower() + @"""
									}
								},
								{
									""term"" : {
										""followers.firstName"" : """ + this._LookFor.ToLower() + @"""
									}
								}
							]
						}
					} }"
			);
			this.TestDefaultAssertions(queryResults);
		}
		[Fact]
		public void FieldQuery()
		{
			var queryResults = this.ConnectedClient.Search<ElasticSearchProject>(
				@" { ""query"" : {
						 ""field"" : { 
							""followers.firstName"" : ""+" + this._LookFor.ToLower() + @" -something else""
						}
					} }"
			);
			this.TestDefaultAssertions(queryResults);
		}
		[Fact]
		public void ExtendedFieldQuery()
		{
			var queryResults = this.ConnectedClient.Search<ElasticSearchProject>(
				@" { ""query"" : {
						""field"" : { 
							""followers.firstName"" : {
								""query"" : ""+" + this._LookFor.ToLower() + @" -something else"",
								""boost"" : 2.0,
								""enable_position_increments"": false
							}
						}
					} }"
			);
			this.TestDefaultAssertions(queryResults);
		}
		[Fact]
		public void FilteredQuery()
		{
			var queryResults = this.ConnectedClient.Search<ElasticSearchProject>(
				@" { ""query"" : {
						""filtered"" : {
							""query"" : {
								""term"" : {
									""followers.firstName"" : """ + this._LookFor.ToLower() + @"""
								}
							},
							""filter"" : {
								""range"" : {
									""id"" : { ""from"" : 1, ""to"" : 20 }
								}
							}
						}
					} }"
			);
			this.TestDefaultAssertions(queryResults);
		}
		[Fact]
		public void FuzzyLikeThisQuery()
		{
			var queryResults = this.ConnectedClient.Search<ElasticSearchProject>(
				@" { ""query"" : {
						""fuzzy_like_this"" : {
							""fields"" : [""_all""],
							""like_text"" : """ + this._LookFor + @"x"",
							""max_query_terms"" : 12
						}
					} }"
			);
			this.TestDefaultAssertions(queryResults);
		}
		[Fact]
		public void FuzzyLikeThisFieldQuery()
		{
			var queryResults = this.ConnectedClient.Search<ElasticSearchProject>(
				@" { ""query"" : {
						 ""fuzzy_like_this_field"" : {
							""followers.firstName"" : {
								""like_text"" : """ + this._LookFor + @"x"",
								""max_query_terms"" : 12
							}
						}
					} }"
			);
			this.TestDefaultAssertions(queryResults);
		}
		[Fact]
		public void FuzzyQuery()
		{
			var queryResults = this.ConnectedClient.Search<ElasticSearchProject>(
				@" { ""query"" : {
						 ""fuzzy"" : {
							""followers.firstName"" : """ + this._LookFor.ToLower() + @"x""
						}
					} }"
			);
			this.TestDefaultAssertions(queryResults);
			Assert.True(queryResults.Documents.First().Followers
				.Any(f => f.FirstName.Equals(this._LookFor, StringComparison.InvariantCultureIgnoreCase)));
		}
		[Fact]
		public void ExtendedFuzzyQuery()
		{
			var queryResults = this.ConnectedClient.Search<ElasticSearchProject>(
				@" { ""query"" : {
						  ""fuzzy"" : { 
							""followers.firstName"" : {
								""value"" : """ + this._LookFor.ToLower() + @"x"",
								""boost"" : 1.0,
								""min_similarity"" : 0.5,
								""prefix_length"" : 0
							}
						}
					} }"
			);
			this.TestDefaultAssertions(queryResults);
			Assert.True(queryResults.Documents.First().Followers
				.Any(f => f.FirstName.Equals(this._LookFor, StringComparison.InvariantCultureIgnoreCase)));
		}

		//TODO: has_child query support!

		[Fact]
		public void MatchAllQuery()
		{
			var queryResults = this.ConnectedClient.Search<ElasticSearchProject>(
				@" { ""query"" : {
						    ""match_all"" : { }
					} }"
			);
			this.TestDefaultAssertions(queryResults);
			Assert.True(queryResults.Total == NestTestData.Data.Count());
			
		}

		[Fact]
		public void MoreLikeThisQuery()
		{
			var queryResults = this.ConnectedClient.Search<ElasticSearchProject>(
				@" { ""query"" : {
						""more_like_this"" : {
							""fields"" : [""_all""],
							""like_text"" : """ + this._LookFor.ToLower() + @""",
							""max_query_terms"" : 12,
							""min_doc_freq"" : 1,
							""min_term_freq"" : 1
						}
					} }"
			);
			this.TestDefaultAssertions(queryResults);
		}
		[Fact]
		public void MoreLikeThisFieldQuery()
		{
			var queryResults = this.ConnectedClient.Search<ElasticSearchProject>(
				@" { ""query"" : {
						 ""more_like_this_field"" : {
							""followers.firstName"" : {
								""like_text"" : """ + this._LookFor + @""",
								""min_doc_freq"" : 1,
								""min_term_freq"" : 1,
								""max_query_terms"" : 12
							}
						}
					} }"
			);
			this.TestDefaultAssertions(queryResults);
		}
		[Fact]
		public void PrefixQuery()
		{
			var queryResults = this.ConnectedClient.Search<ElasticSearchProject>(
				@" { ""query"" : {
						 ""prefix"" : {
							""followers.firstName"" : """ + this._LookFor.Substring(0,4).ToLower() + @"""
						}
					} }"
			);
			this.TestDefaultAssertions(queryResults);
		}
		[Fact]
		public void PrefixExtendedQuery()
		{
			var queryResults = this.ConnectedClient.Search<ElasticSearchProject>(
				@" { ""query"" : {
						 ""prefix"" : {
							""followers.firstName"" : { ""value"" : """ + this._LookFor.Substring(0, 4).ToLower() + @""", ""boost"" : 1.2 }
						}
					} }"
			);
			this.TestDefaultAssertions(queryResults);
		}
		[Fact]
		public void QueryStringQuery()
		{
			var firstFollower = NestTestData.Data.First().Followers.First();
			var firstName = firstFollower.FirstName;
			var lastName = firstFollower.LastName;
			var queryResults = this.ConnectedClient.Search<ElasticSearchProject>(
				@" { ""query"" : {
						""query_string"" : { 
							""default_field"" : ""_all"", 
							""query"" : """+firstName+@" AND "+lastName+@"""
						}
					} }"
			);
			this.TestDefaultAssertions(queryResults);
		}
		[Fact]
		public void QueryStringMultiFieldQuery()
		{
			var firstFollower = NestTestData.Data.First().Followers.First();
			var firstName = firstFollower.FirstName;
			var lastName = firstFollower.LastName;
			var queryResults = this.ConnectedClient.Search<ElasticSearchProject>(
				@" { ""query"" : {
						""query_string"" : { 
							""fields"" : [""followers.firstName"", ""followers.lastName^5""], 
							""query"" : """ + firstName + @" OR " + lastName + @"""
						}
					} }"
			);
			this.TestDefaultAssertions(queryResults);
		}
		
		[Fact]
		public void RangeQuery()
		{
			var queryResults = this.ConnectedClient.Search<ElasticSearchProject>(
				@" { ""query"" : {
						 ""range"" : {
							""id"" : { 
								""from"" : 1, 
								""to"" : 20, 
								""include_lower"" : true, 
								""include_upper"": false, 
								""boost"" : 2.0
							}
						}
					} }"
			);
			this.TestDefaultAssertions(queryResults);
		}

		//TODO: Update test data to include a blob of text so we can write decent span_* queries tests

		[Fact]
		public void TermQuery()
		{
			var queryResults = this.ConnectedClient.Search<ElasticSearchProject>(
				@" { ""query"" : {
						 ""term"" : {
							""followers.firstName"" : """ + this._LookFor.ToLower() + @"""
						}
					} }"
			);
			this.TestDefaultAssertions(queryResults);
			Assert.True(queryResults.Documents.First().Followers
				.Any(f => f.FirstName.Equals(this._LookFor, StringComparison.InvariantCultureIgnoreCase)));
		}
		[Fact]
		public void ExtendedTermQuery()
		{
			var queryResults = this.ConnectedClient.Search<ElasticSearchProject>(
				@" { ""query"" : {
						 ""term"" : {
							""followers.firstName"" : { ""value"" : """ + this._LookFor.ToLower() + @""", ""boost"" : 2.0 }
						}
					} }"
			);
			this.TestDefaultAssertions(queryResults);
			Assert.True(queryResults.Documents.First().Followers
				.Any(f => f.FirstName.Equals(this._LookFor, StringComparison.InvariantCultureIgnoreCase)));
		}
		[Fact]
		public void TermsQuery()
		{
			var firstFollower = NestTestData.Data.First().Followers.First();
			var firstName = firstFollower.FirstName.ToLower();
			var lastName = firstFollower.LastName.ToLower();

			var queryResults = this.ConnectedClient.Search<ElasticSearchProject>(
				@" { ""query"" : {
						 ""terms"" : {
							""followers.firstName"" : [ """ + firstName + @""", """ + lastName + @""" ],
							""minimum_match"" : 1
						}
					} }"
			);
			this.TestDefaultAssertions(queryResults);
			Assert.True(queryResults.Documents.First().Followers
				.Any(f => f.FirstName.Equals(this._LookFor, StringComparison.InvariantCultureIgnoreCase)));
		}

		[Fact]
		public void WildcardQuery()
		{
			var wildcard = this._LookFor.ToLower().Substring(0,this._LookFor.Length -1).Replace("a","?") + "*";
			var queryResults = this.ConnectedClient.Search<ElasticSearchProject>(
				@" { ""query"" : {
						 ""wildcard"" : {
							""followers.firstName"" : """ + wildcard + @"""
						}
					} }"
			);
			this.TestDefaultAssertions(queryResults);
			Assert.True(queryResults.Documents.First().Followers
				.Any(f => f.FirstName.Equals(this._LookFor, StringComparison.InvariantCultureIgnoreCase)));
		}
		[Fact]
		public void WildcardExtendedQuery()
		{
			var wildcard = this._LookFor.ToLower().Substring(0, this._LookFor.Length - 1).Replace("a", "?") + "*";
			var queryResults = this.ConnectedClient.Search<ElasticSearchProject>(
				@" { ""query"" : {
						 ""wildcard"" : {
							""followers.firstName"" : { ""value"" : """ + wildcard + @""", ""boost"" : 2.0 }
						}
					} }"
			);
			this.TestDefaultAssertions(queryResults);
			Assert.True(queryResults.Documents.First().Followers
				.Any(f => f.FirstName.Equals(this._LookFor, StringComparison.InvariantCultureIgnoreCase)));
		}
	}

	//TODO: Implement top_children once we support mapping and mapping of parent child relations.
}
