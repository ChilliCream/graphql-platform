using System.Collections.Generic;
using System.Text;
using ChilliCream.Testing;
using Newtonsoft.Json;
using Snapshooter;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Language
{
    public class Utf8GraphQLRequestParserTests
    {
        [Fact]
        public void Parse_Kitchen_Sink_Query_No_Cache()
        {
            // arrange
            byte[] source = Encoding.UTF8.GetBytes(
                JsonConvert.SerializeObject(
                    new GraphQLRequestDto
                    {
                        Query = FileResource.Open("kitchen-sink.graphql")
                    }));

            // act
            var parserOptions = new ParserOptions();
            var requestParser = new Utf8GraphQLRequestParser(
                source, parserOptions);
            IReadOnlyList<GraphQLRequest> batch = requestParser.Parse();

            // assert
            Assert.Collection(batch,
                r =>
                {
                    Assert.Null(r.OperationName);
                    Assert.Null(r.NamedQuery);
                    Assert.Null(r.Variables);
                    Assert.Null(r.Extensions);

                    QuerySyntaxSerializer.Serialize(r.Query, true)
                        .MatchSnapshot();
                });
        }

        [Fact]
        public void Parse_Kitchen_Sink_Query_With_Cache()
        {
            // arrange
            byte[] source = Encoding.UTF8.GetBytes(
                JsonConvert.SerializeObject(
                    new GraphQLRequestDto
                    {
                        Query = FileResource.Open("kitchen-sink.graphql")
                    }));

            var cache = new DocumentCache();

            var requestParser = new Utf8GraphQLRequestParser(
                source,
                new ParserOptions(),
                cache,
                new Sha1DocumentHashProvider());

            IReadOnlyList<GraphQLRequest> first = requestParser.Parse();

            cache.Add(first[0].NamedQuery, first[0].Query);

            // act
            requestParser = new Utf8GraphQLRequestParser(
                source,
                new ParserOptions(),
                cache,
                new Sha1DocumentHashProvider());

            IReadOnlyList<GraphQLRequest> second = requestParser.Parse();

            // assert
            Assert.Equal(first[0].Query, second[0].Query);
            Assert.Collection(second,
                r =>
                {
                    Assert.Null(r.OperationName);
                    Assert.Null(r.Variables);
                    Assert.Null(r.Extensions);

                    Assert.Equal(r.NamedQuery, "alinKTeX5KKqWsMutgMimKqxU94=");
                    QuerySyntaxSerializer.Serialize(r.Query, true)
                        .MatchSnapshot();
                });
        }

        [Fact]
        public void Parse_Kitchen_Sink_Query_AllProps_No_Cache()
        {
            // arrange
            byte[] source = Encoding.UTF8.GetBytes(
                JsonConvert.SerializeObject(
                    new GraphQLRequestDto
                    {
                        Query = FileResource.Open("kitchen-sink.graphql"),
                        NamedQuery = "ABC",
                        OperationName = "DEF",
                        Variables = new Dictionary<string, object>
                        {
                            { "a" , "b"},
                            { "b" , new Dictionary<string, object>
                                {
                                    { "a" , "b"},
                                    { "b" , true},
                                    { "c" , 1},
                                    { "d" , 1.1},
                                }},
                            { "c" , new List<object>
                                {
                                    new Dictionary<string, object>
                                    {
                                        { "a" , "b"},
                                    }
                                }},
                        },
                        Extensions = new Dictionary<string, object>
                        {
                            { "aa" , "bb"},
                            { "bb" , new Dictionary<string, object>
                                {
                                    { "aa" , "bb"},
                                    { "bb" , true},
                                    { "cc" , 1},
                                    { "df" , 1.1},
                                }},
                            { "cc" , new List<object>
                                {
                                    new Dictionary<string, object>
                                    {
                                        { "aa" , "bb"},
                                    }
                                }},
                        }
                    }));

            // act
            var parserOptions = new ParserOptions();
            var requestParser = new Utf8GraphQLRequestParser(
                source, parserOptions);
            IReadOnlyList<GraphQLRequest> batch = requestParser.Parse();

            // assert
            Assert.Collection(batch,
                r =>
                {
                    Assert.Equal("ABC", r.NamedQuery);
                    Assert.Equal("DEF", r.OperationName);

                    r.Variables.MatchSnapshot(
                        new SnapshotNameExtension("Variables"));
                    r.Extensions.MatchSnapshot(
                        new SnapshotNameExtension("Extensions"));
                    QuerySyntaxSerializer.Serialize(r.Query, true)
                        .MatchSnapshot(new SnapshotNameExtension("Query"));
                });
        }

        private class GraphQLRequestDto
        {
            [JsonProperty("operationName")]
            public string OperationName { get; set; }

            [JsonProperty("namedQuery")]
            public string NamedQuery { get; set; }

            [JsonProperty("query")]
            public string Query { get; set; }

            [JsonProperty("variables")]
            public IReadOnlyDictionary<string, object> Variables { get; set; }

            [JsonProperty("extensions")]
            public IReadOnlyDictionary<string, object> Extensions { get; set; }
        }

        private class DocumentCache
            : IDocumentCache
        {
            private readonly Dictionary<string, DocumentNode> _cache =
                new Dictionary<string, DocumentNode>();

            public void Add(string key, DocumentNode document)
            {
                _cache.Add(key, document);
            }

            public bool TryGetDocument(string key, out DocumentNode document)
            {
                return _cache.TryGetValue(key, out document);
            }
        }
    }
}
