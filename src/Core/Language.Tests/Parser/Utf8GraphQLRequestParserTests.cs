using System.Collections.Generic;
using System.IO;
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
                            .NormalizeLineBreaks()
                    }).NormalizeLineBreaks());

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
                    Assert.Null(r.QueryName);
                    Assert.Null(r.Variables);
                    Assert.Null(r.Extensions);

                    QuerySyntaxSerializer.Serialize(r.Query, true)
                        .MatchSnapshot();
                });
        }

        [Fact]
        public void Parse_Kitchen_Sink_Query_With_Russion_Characters()
        {
            // arrange
            byte[] source = Encoding.UTF8.GetBytes(
                JsonConvert.SerializeObject(
                    new GraphQLRequestDto
                    {
                        Query = FileResource.Open("russion-literals.graphql")
                            .NormalizeLineBreaks()
                    }).NormalizeLineBreaks());

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
                    Assert.Null(r.QueryName);
                    Assert.Null(r.Variables);
                    Assert.Null(r.Extensions);

                    QuerySyntaxSerializer.Serialize(r.Query, true)
                        .MatchSnapshot();
                });
        }

        [Fact]
        public void Parse_Kitchen_Sink_Query_With_Russion_Escaped_Characters()
        {
            // arrange
            byte[] source = Encoding.UTF8.GetBytes(
                FileResource.Open("russion_utf8_escape_characters.json")
                    .NormalizeLineBreaks());

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
                    Assert.Null(r.QueryName);
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
                            .NormalizeLineBreaks()
                    }).NormalizeLineBreaks());

            var cache = new DocumentCache();

            var requestParser = new Utf8GraphQLRequestParser(
                source,
                new ParserOptions(),
                cache,
                new Sha1DocumentHashProvider());

            IReadOnlyList<GraphQLRequest> first = requestParser.Parse();

            cache.Add(first[0].QueryName, first[0].Query);

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

                    Assert.Equal("KwPz8bJWrVDRrtFPjW2sh5CUQwE=", r.QueryName);
                    QuerySyntaxSerializer.Serialize(r.Query, true)
                        .MatchSnapshot();
                });
        }

        [Fact]
        public void Parse_Skip_Custom_Property()
        {
            // arrange
            byte[] source = Encoding.UTF8.GetBytes(
                JsonConvert.SerializeObject(
                    new CustomGraphQLRequestDto
                    {
                        CustomProperty = "FooBar",
                        Query = FileResource.Open("kitchen-sink.graphql")
                            .NormalizeLineBreaks()
                    }).NormalizeLineBreaks());

            var cache = new DocumentCache();

            var requestParser = new Utf8GraphQLRequestParser(
                source,
                new ParserOptions(),
                cache,
                new Sha1DocumentHashProvider());

            // act
            IReadOnlyList<GraphQLRequest> result = requestParser.Parse();

            // assert
            Assert.Collection(result,
                r =>
                {
                    Assert.Null(r.OperationName);
                    Assert.Null(r.Variables);
                    Assert.Null(r.Extensions);

                    Assert.Equal("KwPz8bJWrVDRrtFPjW2sh5CUQwE=", r.QueryName);
                    QuerySyntaxSerializer.Serialize(r.Query, true)
                        .MatchSnapshot();
                });
        }

        [Fact]
        public void Parse_Id_As_Name()
        {
            // arrange
            byte[] source = Encoding.UTF8.GetBytes(
                JsonConvert.SerializeObject(
                    new RelayGraphQLRequestDto
                    {
                        Id = "FooBar",
                        Query = FileResource.Open("kitchen-sink.graphql")
                            .NormalizeLineBreaks()
                    }).NormalizeLineBreaks());

            var cache = new DocumentCache();

            var requestParser = new Utf8GraphQLRequestParser(
                source,
                new ParserOptions(),
                cache,
                new Sha1DocumentHashProvider());

            // act
            IReadOnlyList<GraphQLRequest> result = requestParser.Parse();

            // assert
            Assert.Collection(result,
                r =>
                {
                    Assert.Null(r.OperationName);
                    Assert.Null(r.Variables);
                    Assert.Null(r.Extensions);

                    Assert.Equal("FooBar", r.QueryName);
                    Assert.Equal("KwPz8bJWrVDRrtFPjW2sh5CUQwE=", r.QueryHash);
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
                        Query = FileResource.Open("kitchen-sink.graphql")
                            .NormalizeLineBreaks(),
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
                                        { "ab" , null},
                                        { "ac" , false},
                                    }
                                }},
                        }
                    }).NormalizeLineBreaks());

            // act
            var parserOptions = new ParserOptions();
            var requestParser = new Utf8GraphQLRequestParser(
                source, parserOptions);
            IReadOnlyList<GraphQLRequest> batch = requestParser.Parse();

            // assert
            Assert.Collection(batch,
                r =>
                {
                    Assert.Equal("ABC", r.QueryName);
                    Assert.Equal("DEF", r.OperationName);

                    r.Variables.MatchSnapshot(
                        new SnapshotNameExtension("Variables"));
                    r.Extensions.MatchSnapshot(
                        new SnapshotNameExtension("Extensions"));
                    QuerySyntaxSerializer.Serialize(r.Query, true)
                        .MatchSnapshot(new SnapshotNameExtension("Query"));
                });
        }

        [Fact]
        public void Parse_Json()
        {
            // arrange
            byte[] source = Encoding.UTF8.GetBytes(
                JsonConvert.SerializeObject(
                    new GraphQLRequestDto
                    {
                        Query = FileResource.Open("kitchen-sink.graphql")
                            .NormalizeLineBreaks(),
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
                    }).NormalizeLineBreaks());

            // act
            var parsed = Utf8GraphQLRequestParser.ParseJson(source);

            // assert
            parsed.MatchSnapshot();
        }

        [Fact]
        public void Parse_Socket_Message()
        {
            // arrange
            byte[] source = Encoding.UTF8.GetBytes(
                JsonConvert.SerializeObject(
                    new Dictionary<string, object>
                    {
                        {
                            "payload",
                            new Dictionary<string, object>
                            {
                                { "a" , "b"},
                                { "b" , new Dictionary<string, object>
                                    {
                                        { "a" , "b"},
                                        { "b" , true},
                                        { "c" , 1},
                                        { "d" , 1.1},
                                        { "e" , false},
                                        { "f" , null}
                                    }},
                                { "c" , new List<object>
                                    {
                                        new Dictionary<string, object>
                                        {
                                            { "a" , "b"},
                                        }
                                    }},
                            }
                        },
                        {
                            "type",
                            "foo"
                        },
                        {
                            "id",
                            "bar"
                        }
                    }).NormalizeLineBreaks());

            // act
            GraphQLSocketMessage message =
                Utf8GraphQLRequestParser.ParseMessage(source);

            // assert
            Assert.Equal("foo", message.Type);
            Assert.Equal("bar", message.Id);

            File.WriteAllBytes("Foo.json", message.Payload.ToArray());

            Utf8GraphQLRequestParser.ParseJson(message.Payload).MatchSnapshot();
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

        private class CustomGraphQLRequestDto
            : GraphQLRequestDto
        {
            public string CustomProperty { get; set; }
        }

        private class RelayGraphQLRequestDto
            : GraphQLRequestDto
        {
            [JsonProperty("id")]
            public string Id { get; set; }
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
