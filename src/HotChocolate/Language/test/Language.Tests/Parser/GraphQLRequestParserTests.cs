using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using CookieCrumble;
using Newtonsoft.Json;
using Xunit;

namespace HotChocolate.Language;

public class GraphQLRequestParserTests
{
    [Fact]
    public void Utf8GraphQLRequestParser_Parse()
    {
        // arrange
        var source = Encoding.UTF8.GetBytes(
            JsonConvert.SerializeObject(
                new GraphQLRequestDto
                {
                    Query = FileResource.Open("kitchen-sink.graphql").NormalizeLineBreaks(),
                }).NormalizeLineBreaks());

        // act
        var batch = Utf8GraphQLRequestParser.Parse(source);

        // assert
        Assert.Collection(
            batch,
            r =>
            {
                Assert.Null(r.OperationName);
                Assert.Null(r.QueryId);
                Assert.Null(r.Variables);
                Assert.Null(r.Extensions);
                r.Query.MatchSnapshot();
            });
    }

    [Fact]
    public void Utf8GraphQLRequestParser_ParseJson()
    {
        // arrange
        var source = Encoding.UTF8.GetBytes(
            JsonConvert.SerializeObject(
                new GraphQLRequestDto
                {
                    Query = FileResource.Open("kitchen-sink.graphql")
                        .NormalizeLineBreaks(),
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
        var json = JsonConvert.SerializeObject(
            new GraphQLRequestDto
            {
                Query = FileResource.Open("kitchen-sink.graphql").NormalizeLineBreaks(),
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
        var source = Encoding.UTF8.GetBytes(
            JsonConvert.SerializeObject(
                new GraphQLRequestDto
                {
                    Query = FileResource.Open("kitchen-sink.graphql")
                        .NormalizeLineBreaks(),
                }).NormalizeLineBreaks());

        // act
        var obj =
            Utf8GraphQLRequestParser.ParseJsonObject(source);

        // assert
        obj.MatchSnapshot();
    }

    [Fact]
    public void Utf8GraphQLRequestParser_ParseJsonObject_FromString()
    {
        // arrange
        var json = JsonConvert.SerializeObject(
            new GraphQLRequestDto
            {
                Query = FileResource.Open("kitchen-sink.graphql")
                    .NormalizeLineBreaks(),
            }).NormalizeLineBreaks();

        // act
        var obj =
            Utf8GraphQLRequestParser.ParseJsonObject(json);

        // assert
        obj.MatchSnapshot();
    }

    [Fact]
    public void Parse_Kitchen_Sink_Query_No_Cache()
    {
        // arrange
        var source = Encoding.UTF8.GetBytes(
            JsonConvert.SerializeObject(
                new GraphQLRequestDto
                {
                    Query = FileResource.Open("kitchen-sink.graphql")
                        .NormalizeLineBreaks(),
                }).NormalizeLineBreaks());

        // act
        var parserOptions = new ParserOptions();
        var requestParser = new Utf8GraphQLRequestParser(
            source, parserOptions);
        var batch = requestParser.Parse();

        // assert
        Assert.Collection(batch,
            r =>
            {
                Assert.Null(r.OperationName);
                Assert.Null(r.QueryId);
                Assert.Null(r.Variables);
                Assert.Null(r.Extensions);
                r.Query.MatchSnapshot();
            });
    }

    [Fact]
    public void Parse_Kitchen_Sink_Query_With_Russian_Characters()
    {
        // arrange
        var source = Encoding.UTF8.GetBytes(
            JsonConvert.SerializeObject(
                new GraphQLRequestDto
                {
                    Query = FileResource.Open("russian-literals.graphql").NormalizeLineBreaks(),
                }).NormalizeLineBreaks());

        // act
        var parserOptions = new ParserOptions();
        var requestParser = new Utf8GraphQLRequestParser(
            source, parserOptions);
        var batch = requestParser.Parse();

        // assert
        Assert.Collection(batch,
            r =>
            {
                Assert.Null(r.OperationName);
                Assert.Null(r.QueryId);
                Assert.Null(r.Variables);
                Assert.Null(r.Extensions);

                r.Query.MatchSnapshot();
            });
    }

    [Fact]
    public void Parse_Kitchen_Sink_Query_With_Russian_Escaped_Characters()
    {
        // arrange
        var source = Encoding.UTF8.GetBytes(
            FileResource.Open("russian_utf8_escape_characters.json")
                .NormalizeLineBreaks());

        // act
        var parserOptions = new ParserOptions();
        var requestParser = new Utf8GraphQLRequestParser(
            source, parserOptions);
        var batch = requestParser.Parse();

        // assert
        Assert.Collection(batch,
            r =>
            {
                Assert.Null(r.OperationName);
                Assert.Null(r.QueryId);
                Assert.Null(r.Variables);
                Assert.Null(r.Extensions);

                r.Query.MatchSnapshot();
            });
    }

    [Fact]
    public void Parse_Kitchen_Sink_Query_With_Cache()
    {
        // arrange
        var request = new GraphQLRequestDto
        {
            Query = FileResource.Open("kitchen-sink.graphql")
                .NormalizeLineBreaks(),
        };

        var buffer = Encoding.UTF8.GetBytes(request.Query);
        var expectedHash = Convert.ToBase64String(
            SHA1.Create().ComputeHash(buffer))
            .Replace("/", "_")
            .Replace("+", "-")
            .TrimEnd('=');
            
        var source = Encoding.UTF8.GetBytes(
            JsonConvert.SerializeObject(request).NormalizeLineBreaks());

        var cache = new DocumentCache();

        var requestParser = new Utf8GraphQLRequestParser(
            source,
            new ParserOptions(),
            cache,
            new Sha1DocumentHashProvider());

        var first = requestParser.Parse();

        cache.TryAddDocument(first[0].QueryId, first[0].Query);

        // act
        requestParser = new Utf8GraphQLRequestParser(
            source,
            new ParserOptions(),
            cache,
            new Sha1DocumentHashProvider());

        var second = requestParser.Parse();

        // assert
        Assert.Equal(first[0].Query, second[0].Query);
        Assert.Collection(second,
            r =>
            {
                Assert.Null(r.OperationName);
                Assert.Null(r.Variables);
                Assert.Null(r.Extensions);

                Assert.Equal(expectedHash, r.QueryId);
                r.Query.MatchSnapshot();
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
                .NormalizeLineBreaks(),
        };

        var source = Encoding.UTF8.GetBytes(
            JsonConvert.SerializeObject(request
                ).NormalizeLineBreaks());

        var buffer = Encoding.UTF8.GetBytes(request.Query);
        var expectedHash = Convert.ToBase64String(
            SHA1.Create().ComputeHash(buffer))
            .Replace("/", "_")
            .Replace("+", "-")
            .TrimEnd('=');
        
        var cache = new DocumentCache();

        var requestParser = new Utf8GraphQLRequestParser(
            source,
            new ParserOptions(),
            cache,
            new Sha1DocumentHashProvider());

        // act
        var result = requestParser.Parse();

        // assert
        Assert.Collection(result,
            r =>
            {
                Assert.Null(r.OperationName);
                Assert.Null(r.Variables);
                Assert.Null(r.Extensions);

                Assert.Equal(expectedHash, r.QueryId);
                r.Query.MatchSnapshot();
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
                .NormalizeLineBreaks(),
        };

        var source = Encoding.UTF8.GetBytes(
            JsonConvert.SerializeObject(request
                ).NormalizeLineBreaks());

        var buffer = Encoding.UTF8.GetBytes(request.Query);
        var expectedHash = Convert.ToBase64String(
            SHA1.Create().ComputeHash(buffer))
            .Replace("/", "_")
            .Replace("+", "-")
            .TrimEnd('=');
        
        var cache = new DocumentCache();

        var requestParser = new Utf8GraphQLRequestParser(
            source,
            new ParserOptions(),
            cache,
            new Sha1DocumentHashProvider());

        // act
        var result = requestParser.Parse();

        // assert
        Assert.Collection(result,
            r =>
            {
                Assert.Null(r.OperationName);
                Assert.Null(r.Variables);
                Assert.Null(r.Extensions);

                Assert.Equal("FooBar", r.QueryId);
                Assert.Equal(expectedHash, r.QueryHash);
                r.Query.MatchSnapshot();
            });
    }

    [Fact]
    public void Parse_Kitchen_Sink_Query_AllProps_No_Cache()
    {
        // arrange
        var source = Encoding.UTF8.GetBytes(
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
                                    },
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
                                    },
                                }},
                    },
                }).NormalizeLineBreaks());

        // act
        var parserOptions = new ParserOptions();
        var requestParser = new Utf8GraphQLRequestParser(
            source, parserOptions);
        var batch = requestParser.Parse();

        // assert
        var snapshot = new Snapshot();
        Assert.Collection(batch,
            r =>
            {
                Assert.Equal("ABC", r.QueryId);
                Assert.Equal("DEF", r.OperationName);

                snapshot.Add(r.Variables, "Variables:");
                snapshot.Add(r.Extensions, "Extensions:");
                snapshot.Add(r.Query, "Query:");
            });
        snapshot.Match();
    }

    [Fact]
    public void Parse_Json()
    {
        // arrange
        var source = Encoding.UTF8.GetBytes(
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
                                    },
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
                                    },
                                }},
                    },
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
        var source = Encoding.UTF8.GetBytes(
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
                                        { "f" , null},
                                    }},
                                { "c" , new List<object>
                                    {
                                        new Dictionary<string, object>
                                        {
                                            { "a" , "b"},
                                        },
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
                        },
                }).NormalizeLineBreaks());

        // act
        var message =
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
        var source = Encoding.UTF8.GetBytes(
            FileResource.Open("Apollo_AQP_QuerySignature_1.json")
                .NormalizeLineBreaks());

        // act
        var parserOptions = new ParserOptions();
        var requestParser = new Utf8GraphQLRequestParser(
            source,
            parserOptions,
            new DocumentCache(),
            new Sha256DocumentHashProvider());
        var batch = requestParser.Parse();

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
        var source = Encoding.UTF8.GetBytes(
            FileResource.Open("Apollo_AQP_QuerySignature_2.json")
                .NormalizeLineBreaks());

        // act
        var parserOptions = new ParserOptions();
        var requestParser = new Utf8GraphQLRequestParser(
            source,
            parserOptions,
            new DocumentCache(),
            new Sha256DocumentHashProvider());
        var batch = requestParser.Parse();

        // assert
        Assert.Collection(batch,
            r =>
            {
                Assert.Null(r.OperationName);
                Assert.Equal("hashOfQuery", r.QueryId);
                Assert.Collection(r.Variables!, Assert.Empty);
                Assert.True(r.Extensions!.ContainsKey("persistedQuery"));
                Assert.Null(r.Query);
                Assert.Null(r.QueryHash);
            });
    }

    [Fact]
    public void Parse_Apollo_AQP_FullRequest_And_Verify_Hash()
    {
        // arrange
        var source = Encoding.UTF8.GetBytes(
            FileResource.Open("Apollo_AQP_FullRequest.json")
                .NormalizeLineBreaks());

        // act
        var parserOptions = new ParserOptions();
        var requestParser = new Utf8GraphQLRequestParser(
            source,
            parserOptions,
            new DocumentCache(),
            new Sha256DocumentHashProvider(HashFormat.Hex));
        var batch = requestParser.Parse();

        // assert
        Assert.Collection(batch,
            r =>
            {
                Assert.Null(r.OperationName);
                Assert.Collection(r.Variables!, Assert.Empty);
                Assert.True(r.Extensions!.ContainsKey("persistedQuery"));
                Assert.NotNull(r.Query);

                if (r.Extensions.TryGetValue("persistedQuery", out var o)
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
        var source = Encoding.UTF8.GetBytes(
            FileResource.Open("Float.json")
            .NormalizeLineBreaks());

        // act
        var obj = Utf8GraphQLRequestParser.ParseJson(source);

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
                var source = Encoding.UTF8.GetBytes("{\"query\":\"\"}"
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
                var source = Encoding.UTF8.GetBytes("{ }"
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
                var source = Encoding.UTF8.GetBytes(string.Empty);
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
                var source = Encoding.UTF8.GetBytes(" ");
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

    private sealed class CustomGraphQLRequestDto
        : GraphQLRequestDto
    {
        public string CustomProperty { get; set; }
    }

    private sealed class RelayGraphQLRequestDto
        : GraphQLRequestDto
    {
        [JsonProperty("id")]
        public new string Id { get; set; }
    }

    private sealed class DocumentCache : IDocumentCache
    {
        private readonly Dictionary<string, DocumentNode> _cache = new();

        public int Capacity => int.MaxValue;

        public int Count => _cache.Count;

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

        public void Clear()
        {
            _cache.Clear();
        }
    }
}
