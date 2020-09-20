using System;
using System.Security.Cryptography;
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
        public void Utf8GraphQLRequestParser_Parse()
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
            IReadOnlyList<GraphQLRequest> batch =
                Utf8GraphQLRequestParser.Parse(source);

            // assert
            Assert.Collection(batch,
                r =>
                {
                    Assert.Null(r.OperationName);
                    Assert.Null(r.QueryId);
                    Assert.Null(r.Variables);
                    Assert.Null(r.Extensions);

                    QuerySyntaxSerializer.Serialize(r.Query!, true)
                        .MatchSnapshot();
                });
        }

        [Fact]
        public void Utf8GraphQLRequestParser_ParseJson()
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
            var obj = Utf8GraphQLRequestParser.ParseJson(source);

            // assert
            obj.MatchSnapshot();
        }

        [Fact]
        public void Utf8GraphQLRequestParser_ParseJson_FromString()
        {
            // arrange
            string json = JsonConvert.SerializeObject(
                new GraphQLRequestDto
                {
                    Query = FileResource.Open("kitchen-sink.graphql")
                        .NormalizeLineBreaks()
                }).NormalizeLineBreaks();

            // act
            var obj = Utf8GraphQLRequestParser.ParseJson(json);

            // assert
            obj.MatchSnapshot();
        }

        [Fact]
        public void Utf8GraphQLRequestParser_ParseJsonObject()
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
            var obj = Utf8GraphQLRequestParser.ParseJsonObject(source);

            // assert
            obj.MatchSnapshot();
        }

        [Fact]
        public void Utf8GraphQLRequestParser_ParseJsonObject_FromString()
        {
            // arrange
            string json = JsonConvert.SerializeObject(
                new GraphQLRequestDto
                {
                    Query = FileResource.Open("kitchen-sink.graphql")
                        .NormalizeLineBreaks()
                }).NormalizeLineBreaks();

            // act
            var obj = Utf8GraphQLRequestParser.ParseJsonObject(json);

            // assert
            obj.MatchSnapshot();
        }

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
                    Assert.Null(r.QueryId);
                    Assert.Null(r.Variables);
                    Assert.Null(r.Extensions);

                    QuerySyntaxSerializer.Serialize(r.Query!, true)
                        .MatchSnapshot();
                });
        }

        [Fact]
        public void Parse_Kitchen_Sink_Query_With_Russian_Characters()
        {
            // arrange
            byte[] source = Encoding.UTF8.GetBytes(
                JsonConvert.SerializeObject(
                    new GraphQLRequestDto
                    {
                        Query = FileResource.Open("russian-literals.graphql")
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
                    Assert.Null(r.QueryId);
                    Assert.Null(r.Variables);
                    Assert.Null(r.Extensions);

                    QuerySyntaxSerializer.Serialize(r.Query!, true)
                        .MatchSnapshot();
                });
        }

        [Fact]
        public void Parse_Kitchen_Sink_Query_With_Russian_Escaped_Characters()
        {
            // arrange
            byte[] source = Encoding.UTF8.GetBytes(
                FileResource.Open("russian_utf8_escape_characters.json")
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
                    Assert.Null(r.QueryId);
                    Assert.Null(r.Variables);
                    Assert.Null(r.Extensions);

                    QuerySyntaxSerializer.Serialize(r.Query!, true)
                        .MatchSnapshot();
                });
        }

        [Fact]
        public void Parse_Kitchen_Sink_Query_With_Cache()
        {
            // arrange
            var request = new GraphQLRequestDto
            {
                Query = FileResource.Open("kitchen-sink.graphql")
                    .NormalizeLineBreaks()
            };

            byte[] buffer = Encoding.UTF8.GetBytes(request.Query);
            var expectedHash = Convert.ToBase64String(
                SHA1.Create().ComputeHash(buffer));

            byte[] source = Encoding.UTF8.GetBytes(
                JsonConvert.SerializeObject(request
                    ).NormalizeLineBreaks());

            var cache = new DocumentCache();

            var requestParser = new Utf8GraphQLRequestParser(
                source,
                new ParserOptions(),
                cache,
                new Sha1DocumentHashProvider());

            IReadOnlyList<GraphQLRequest> first = requestParser.Parse();

            cache.TryAddDocument(first[0].QueryId, first[0].Query);

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

                    Assert.Equal(expectedHash, r.QueryId);
                    QuerySyntaxSerializer.Serialize(r.Query!, true)
                        .MatchSnapshot();
                });
        }

        [Fact]
        public void Parse_Skip_Custom_Property()
        {
            // arrange
            var request = new CustomGraphQLRequestDto
            {
                CustomProperty = "FooBar",
                Query = FileResource.Open("kitchen-sink.graphql")
                    .NormalizeLineBreaks()
            };

            byte[] source = Encoding.UTF8.GetBytes(
                JsonConvert.SerializeObject(request
                    ).NormalizeLineBreaks());

            byte[] buffer = Encoding.UTF8.GetBytes(request.Query);
            var expectedHash = Convert.ToBase64String(
                SHA1.Create().ComputeHash(buffer));

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

                    Assert.Equal(expectedHash, r.QueryId);
                    QuerySyntaxSerializer.Serialize(r.Query!, true)
                        .MatchSnapshot();
                });
        }

        [Fact]
        public void Parse_Id_As_Name()
        {
            // arrange
            var request = new RelayGraphQLRequestDto
            {
                Id = "FooBar",
                Query = FileResource.Open("kitchen-sink.graphql")
                    .NormalizeLineBreaks()
            };

            byte[] source = Encoding.UTF8.GetBytes(
                JsonConvert.SerializeObject(request
                    ).NormalizeLineBreaks());

            byte[] buffer = Encoding.UTF8.GetBytes(request.Query);
            var expectedHash = Convert.ToBase64String(
                SHA1.Create().ComputeHash(buffer));

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

                    Assert.Equal("FooBar", r.QueryId);
                    Assert.Equal(expectedHash, r.QueryHash);
                    QuerySyntaxSerializer.Serialize(r.Query!, true)
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
                        Query = FileResource.Open("kitchen-sink.graphql").NormalizeLineBreaks(),
                        Id = "ABC",
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
                    Assert.Equal("ABC", r.QueryId);
                    Assert.Equal("DEF", r.OperationName);

                    r.Variables.MatchSnapshot(
                        new SnapshotNameExtension("Variables"));
                    r.Extensions.MatchSnapshot(
                        new SnapshotNameExtension("Extensions"));
                    QuerySyntaxSerializer.Serialize(r.Query!, true)
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
                        Id = "ABC",
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
            object parsed = Utf8GraphQLRequestParser.ParseJson(source);

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

        [Fact]
        public void Parse_Apollo_AQP_SignatureQuery()
        {
            // arrange
            byte[] source = Encoding.UTF8.GetBytes(
                FileResource.Open("Apollo_AQP_QuerySignature_1.json")
                    .NormalizeLineBreaks());

            // act
            var parserOptions = new ParserOptions();
            var requestParser = new Utf8GraphQLRequestParser(
                source,
                parserOptions,
                new DocumentCache(),
                new Sha256DocumentHashProvider());
            IReadOnlyList<GraphQLRequest> batch = requestParser.Parse();

            // assert
            Assert.Collection(batch,
                r =>
                {
                    Assert.Equal("MyQuery", r.OperationName);
                    Assert.Equal("hashOfQuery", r.QueryId);
                    Assert.Null(r.Variables);
                    Assert.True(r.Extensions!.ContainsKey("persistedQuery"));
                    Assert.Null(r.Query);
                    Assert.Null(r.QueryHash);
                });
        }

        [Fact]
        public void Parse_Apollo_AQP_SignatureQuery_Variables_Without_Values()
        {
            // arrange
            byte[] source = Encoding.UTF8.GetBytes(
                FileResource.Open("Apollo_AQP_QuerySignature_2.json")
                    .NormalizeLineBreaks());

            // act
            var parserOptions = new ParserOptions();
            var requestParser = new Utf8GraphQLRequestParser(
                source,
                parserOptions,
                new DocumentCache(),
                new Sha256DocumentHashProvider());
            IReadOnlyList<GraphQLRequest> batch = requestParser.Parse();

            // assert
            Assert.Collection(batch,
                r =>
                {
                    Assert.Null(r.OperationName);
                    Assert.Equal("hashOfQuery", r.QueryId);
                    Assert.Empty(r.Variables!);
                    Assert.True(r.Extensions!.ContainsKey("persistedQuery"));
                    Assert.Null(r.Query);
                    Assert.Null(r.QueryHash);
                });
        }

        [Fact]
        public void Parse_Apollo_AQP_FullRequest_And_Verify_Hash()
        {
            // arrange
            byte[] source = Encoding.UTF8.GetBytes(
                FileResource.Open("Apollo_AQP_FullRequest.json")
                    .NormalizeLineBreaks());

            // act
            var parserOptions = new ParserOptions();
            var requestParser = new Utf8GraphQLRequestParser(
                source,
                parserOptions,
                new DocumentCache(),
                new Sha256DocumentHashProvider(HashFormat.Hex));
            IReadOnlyList<GraphQLRequest> batch = requestParser.Parse();

            // assert
            Assert.Collection(batch,
                r =>
                {
                    Assert.Null(r.OperationName);
                    Assert.Empty(r.Variables!);
                    Assert.True(r.Extensions!.ContainsKey("persistedQuery"));
                    Assert.NotNull(r.Query);

                    if (r.Extensions.TryGetValue("persistedQuery", out object o)
                        && o is IReadOnlyDictionary<string, object> persistedQuery
                        && persistedQuery.TryGetValue("sha256Hash", out o)
                        && o is string hash)
                    {
                        Assert.Equal(hash, r.QueryHash);
                    }
                });
        }

        [Fact]
        public void Parse_Float_Exponent_Format()
        {
            // arrange
            byte[] source = Encoding.UTF8.GetBytes(
                FileResource.Open("Float.json")
                .NormalizeLineBreaks());

            // act
            object obj = Utf8GraphQLRequestParser.ParseJson(source);

            // assert
            obj.MatchSnapshot();
        }

        [Fact]
        public void Parse_Invalid_Query()
        {
            // assert
            Assert.Throws<SyntaxException>(
                () =>
                {
                    // arrange
                    byte[] source = Encoding.UTF8.GetBytes("{\"query\":\"\"}"
                        .NormalizeLineBreaks());
                    var parserOptions = new ParserOptions();
                    var requestParser = new Utf8GraphQLRequestParser(
                        source,
                        parserOptions,
                        new DocumentCache(),
                        new Sha256DocumentHashProvider());

                    // act
                    requestParser.Parse();
                });
        }

        [Fact]
        public void Parse_Empty_Json()
        {
            // assert
            Assert.Throws<SyntaxException>(
                () =>
                {
                    // arrange
                    byte[] source = Encoding.UTF8.GetBytes("{ }"
                        .NormalizeLineBreaks());
                    var parserOptions = new ParserOptions();
                    var requestParser = new Utf8GraphQLRequestParser(
                        source,
                        parserOptions,
                        new DocumentCache(),
                        new Sha256DocumentHashProvider());

                    // act
                    requestParser.Parse();
                });
        }

        [Fact]
        public void Parse_Empty_String()
        {
            // assert
            Assert.Throws<ArgumentException>(
                () =>
                {
                    // arrange
                    byte[] source = Encoding.UTF8.GetBytes(""
                        .NormalizeLineBreaks());
                    var parserOptions = new ParserOptions();
                    var requestParser = new Utf8GraphQLRequestParser(
                        source,
                        parserOptions,
                        new DocumentCache(),
                        new Sha256DocumentHashProvider());

                    // act
                    requestParser.Parse();
                });
        }

        [Fact]
        public void Parse_Space_String()
        {
            // assert
            Assert.Throws<SyntaxException>(
                () =>
                {
                    // arrange
                    byte[] source = Encoding.UTF8.GetBytes(" "
                        .NormalizeLineBreaks());
                    var parserOptions = new ParserOptions();
                    var requestParser = new Utf8GraphQLRequestParser(
                        source,
                        parserOptions,
                        new DocumentCache(),
                        new Sha256DocumentHashProvider());

                    // act
                    requestParser.Parse();
                });
        }

        private class GraphQLRequestDto
        {
            [JsonProperty("operationName")]
            public string OperationName { get; set; }

            [JsonProperty("id")]
            public string Id { get; set; }

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

            public void TryAddDocument(string documentId, DocumentNode document)
            {
                if (!_cache.ContainsKey(documentId))
                {
                    _cache.Add(documentId, document);
                }
            }

            public bool TryGetDocument(
                string documentId,
                out DocumentNode document) =>
                _cache.TryGetValue(documentId, out document);
        }
    }
}
