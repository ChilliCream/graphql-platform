using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Newtonsoft.Json;

namespace HotChocolate.Language;

public class Utf8GraphQLRequestParserTests
{
    [Fact]
    public void Utf8GraphQLRequestParser_Parse()
    {
        // arrange
        var source = Encoding.UTF8.GetBytes(
            JsonConvert.SerializeObject(
                new GraphQLRequestDto(
                    query: FileResource.Open("kitchen-sink.graphql").NormalizeLineBreaks()))
                .NormalizeLineBreaks());

        // act
        var batch = Utf8GraphQLRequestParser.Parse(source);

        // assert
        var r = Assert.Single(batch);
        Assert.Null(r.OperationName);
        Assert.Null(r.DocumentId);
        Assert.Null(r.Variables);
        Assert.Null(r.Extensions);
        r.Document.MatchSnapshot();
    }

    [Fact]
    public void Parse_Large_Query_Sequence()
    {
        // arrange
        var pipe = new Pipe();
        pipe.Writer.Write("{ \"query\": \"{ "u8);
        for (var i = 0; i < 1_000; i++)
        {
            pipe.Writer.Write("aReallyLongFieldNameToFillUpTheSequences "u8);
        }
        pipe.Writer.Write("}\" }"u8);
        pipe.Writer.Complete();
        pipe.Reader.TryRead(out var result);

        // act
        var batch = Utf8GraphQLRequestParser.Parse(result.Buffer);

        // assert
        var r = Assert.Single(batch);
        Assert.Null(r.OperationName);
        Assert.Null(r.DocumentId);
        Assert.Null(r.Variables);
        Assert.Null(r.Extensions);
        r.Document.MatchSnapshot();
    }

    [Fact]
    public void Parse_Kitchen_Sink_Query_No_Cache()
    {
        // arrange
        var source = Encoding.UTF8.GetBytes(
            JsonConvert.SerializeObject(
                new GraphQLRequestDto(
                    query: FileResource.Open("kitchen-sink.graphql").NormalizeLineBreaks()))
                .NormalizeLineBreaks());

        // act
        var parserOptions = new ParserOptions();
        var requestParser = new Utf8GraphQLRequestParser(parserOptions);
        var batch = requestParser.Parse(source);

        // assert
        var request = Assert.Single(batch);
        Assert.Null(request.OperationName);
        Assert.Null(request.DocumentId);
        Assert.Null(request.Variables);
        Assert.Null(request.Extensions);
        request.Document.MatchSnapshot();
    }

    [Fact]
    public void Parse_Kitchen_Sink_Query_With_Russian_Characters()
    {
        // arrange
        var source = Encoding.UTF8.GetBytes(
            JsonConvert.SerializeObject(
                new GraphQLRequestDto(
                    query: FileResource.Open("russian-literals.graphql").NormalizeLineBreaks()))
                .NormalizeLineBreaks());

        // act
        var parserOptions = new ParserOptions();
        var requestParser = new Utf8GraphQLRequestParser(parserOptions);
        var batch = requestParser.Parse(source);

        // assert
        Assert.Collection(batch,
            r =>
            {
                Assert.Null(r.OperationName);
                Assert.Null(r.DocumentId);
                Assert.Null(r.Variables);
                Assert.Null(r.Extensions);

                r.Document.MatchSnapshot();
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
        var requestParser = new Utf8GraphQLRequestParser(parserOptions);
        var batch = requestParser.Parse(source);

        // assert
        var request = Assert.Single(batch);
        Assert.Null(request.OperationName);
        Assert.Null(request.DocumentId);
        Assert.Null(request.Variables);
        Assert.Null(request.Extensions);

        request.Document.MatchSnapshot();
    }

    [Fact]
    public void Parse_Kitchen_Sink_Query_With_Cache()
    {
        // arrange
        var request = new GraphQLRequestDto(
            query: FileResource.Open("kitchen-sink.graphql").NormalizeLineBreaks());

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
            new ParserOptions(),
            cache,
            new Sha1DocumentHashProvider());

        var first = requestParser.Parse(source);

        cache.TryAddDocument(
            first[0].DocumentId?.Value!,
            new CachedDocument(first[0].Document!, OperationDocumentHash.Empty, false));

        // act
        requestParser = new Utf8GraphQLRequestParser(
            new ParserOptions(),
            cache,
            new Sha1DocumentHashProvider());

        var second = requestParser.Parse(source);

        // assert
        Assert.Equal(first[0].Document, second[0].Document);
        Assert.Collection(second,
            r =>
            {
                Assert.Null(r.OperationName);
                Assert.Null(r.Variables);
                Assert.Null(r.Extensions);

                Assert.Equal(expectedHash, r.DocumentId?.Value);
                r.Document.MatchSnapshot();
            });
    }

    [Fact]
    public void Parse_Unknown_Property_Throws()
    {
        // arrange
        var request = new CustomGraphQLRequestDto(
            customProperty: "FooBar",
            query: FileResource.Open("kitchen-sink.graphql").NormalizeLineBreaks());

        var source = Encoding.UTF8.GetBytes(
            JsonConvert.SerializeObject(request).NormalizeLineBreaks());

        // act & assert
        var exception = Assert.Throws<InvalidGraphQLRequestException>(
            () => Utf8GraphQLRequestParser.Parse(source));
        Assert.Contains("CustomProperty", exception.Message);
    }

    [Fact]
    public void Parse_Id_As_Name()
    {
        // arrange
        var request = new RelayGraphQLRequestDto(
            id: "FooBar",
            query: FileResource.Open("kitchen-sink.graphql").NormalizeLineBreaks());

        var source = Encoding.UTF8.GetBytes(
            JsonConvert.SerializeObject(request
                ).NormalizeLineBreaks());

        var buffer = Encoding.UTF8.GetBytes(request.Query);
        var expectedHash = Convert.ToBase64String(
            SHA1.HashData(buffer))
            .Replace("/", "_")
            .Replace("+", "-")
            .TrimEnd('=');

        var cache = new DocumentCache();

        var requestParser = new Utf8GraphQLRequestParser(
            new ParserOptions(),
            cache,
            new Sha1DocumentHashProvider());

        // act
        var result = requestParser.Parse(source);

        // assert
        Assert.Collection(result,
            r =>
            {
                Assert.Null(r.OperationName);
                Assert.Null(r.Variables);
                Assert.Null(r.Extensions);

                Assert.Equal("FooBar", r.DocumentId?.Value);
                Assert.Equal(expectedHash, r.DocumentHash?.Value);
                r.Document.MatchSnapshot();
            });
    }

    [Theory]
    [InlineData("PROPAGATE", ErrorHandlingMode.Propagate)]
    [InlineData("NULL", ErrorHandlingMode.Null)]
    [InlineData("HALT", ErrorHandlingMode.Halt)]
    [InlineData("propagate", ErrorHandlingMode.Propagate)]
    [InlineData("null", ErrorHandlingMode.Null)]
    [InlineData("halt", ErrorHandlingMode.Halt)]
    [InlineData(null, null)]
    [InlineData("bla", null)]
    public void Parse_OnError(string? onError, ErrorHandlingMode? expectedErrorHandlingMode)
    {
        // arrange
        var request = new GraphQLRequestDto(
            query: FileResource.Open("kitchen-sink.graphql").NormalizeLineBreaks(),
            onError: onError);

        var source = Encoding.UTF8.GetBytes(
            JsonConvert.SerializeObject(request
            ).NormalizeLineBreaks());

        var requestParser = new Utf8GraphQLRequestParser();

        // act
        var result = requestParser.Parse(source);

        // assert
        Assert.Collection(result,
            r =>
            {
                Assert.Null(r.OperationName);
                Assert.Null(r.DocumentId);
                Assert.Null(r.Variables);
                Assert.Null(r.Extensions);

                Assert.Equal(expectedErrorHandlingMode, r.ErrorHandlingMode);
            });
    }

    [Fact]
    public void Parse_Kitchen_Sink_Query_AllProps_No_Cache()
    {
        // arrange
        var source = Encoding.UTF8.GetBytes(
            JsonConvert.SerializeObject(
                new GraphQLRequestDto(
                    query: FileResource.Open("kitchen-sink.graphql").NormalizeLineBreaks(),
                    id: "ABC",
                    operationName: "DEF",
                    variables: new Dictionary<string, object>
                    {
                        { "a", "b" },
                        {
                            "b",
                            new Dictionary<string, object>
                            {
                                { "a", "b" },
                                { "b", true },
                                { "c", 1 },
                                { "d", 1.1 }
                            }
                        },
                        {
                            "c",
                            new List<object>
                            {
                                new Dictionary<string, object>
                                {
                                    { "a", "b" }
                                }
                            }
                        }
                    },
                    extensions: new Dictionary<string, object>
                    {
                        { "aa", "bb" },
                        {
                            "bb",
                            new Dictionary<string, object>
                            {
                                { "aa", "bb" },
                                { "bb", true },
                                { "cc", 1 },
                                { "df", 1.1 }
                            }
                        },
                        {
                            "cc",
                            new List<object>
                            {
                                new Dictionary<string, object?>
                                {
                                    { "aa", "bb" },
                                    { "ab", null },
                                    { "ac", false }
                                }
                            }
                        }
                    })).NormalizeLineBreaks());

        // act
        var parserOptions = new ParserOptions();
        var requestParser = new Utf8GraphQLRequestParser(parserOptions);
        var batch = requestParser.Parse(source);

        // assert
        var snapshot = new Snapshot();
        Assert.Collection(batch,
            r =>
            {
                Assert.Equal("ABC", r.DocumentId?.Value);
                Assert.Equal("DEF", r.OperationName);

                snapshot.Add(r.Variables, "Variables:");
                snapshot.Add(r.Extensions, "Extensions:");
                snapshot.Add(r.Document, "Query:");
            });
        snapshot.Match();
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
                                { "b" , new Dictionary<string, object?>
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
                                            { "a" , "b"}
                                        }
                                    }}
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
        var message = Utf8GraphQLSocketMessageParser.ParseMessage(source);

        // assert
        Assert.Equal("foo", message.Type);
        Assert.Equal("bar", message.Id);
        Assert.False(message.Payload.IsEmpty);
    }

    [Fact]
    public void Parse_Apollo_Client_v4_Query()
    {
        // arrange
        var requestData = """
            {
                "id": "foo",
                "query": "subscription OnEvent { fooChanged }",
                "operationName": "OnEvent",
                "operationType": "subscription"
            }
            """u8;

        // act
        var result = Utf8GraphQLRequestParser.Parse(requestData);

        // assert
        result.MatchSnapshot();
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
            parserOptions,
            new DocumentCache(),
            new Sha256DocumentHashProvider());
        var batch = requestParser.Parse(source);

        // assert
        var request = Assert.Single(batch);
        Assert.Equal("MyQuery", request.OperationName);
        Assert.Equal("hashOfQuery", request.DocumentId?.Value);
        Assert.Null(request.Variables);
        Assert.True(request.Extensions!.RootElement.TryGetProperty("persistedQuery", out _));
        Assert.Null(request.Document);
        Assert.Equal("hashOfQuery", request.DocumentHash?.Value);
        Assert.Equal("sha256Hash", request.DocumentHash?.AlgorithmName);
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
            parserOptions,
            new DocumentCache(),
            new Sha256DocumentHashProvider());
        var batch = requestParser.Parse(source);

        // assert
        var r = Assert.Single(batch);
        Assert.Null(r.OperationName);
        Assert.Equal("hashOfQuery", r.DocumentId?.Value);
        Assert.NotNull(r.Variables);
        Assert.Empty(r.Variables.RootElement.EnumerateObject());
        Assert.True(r.Extensions!.RootElement.TryGetProperty("persistedQuery", out _));
        Assert.Null(r.Document);
        Assert.Equal("hashOfQuery", r.DocumentHash?.Value);
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
            parserOptions,
            new DocumentCache(),
            new Sha256DocumentHashProvider(HashFormat.Hex));
        var batch = requestParser.Parse(source);

        // assert
        Assert.Collection(batch,
            r =>
            {
                Assert.Null(r.OperationName);
                Assert.NotNull(r.Variables);
                Assert.Empty(r.Variables.RootElement.EnumerateObject());
                Assert.True(r.Extensions!.RootElement.TryGetProperty("persistedQuery", out _));
                Assert.NotNull(r.Document);

                if (r.Extensions.RootElement.TryGetProperty("persistedQuery", out var persistedQuery)
                    && persistedQuery.ValueKind == JsonValueKind.Object
                    && persistedQuery.TryGetProperty("sha256Hash", out var hashElement)
                    && hashElement.ValueKind == JsonValueKind.String)
                {
                    Assert.Equal(hashElement.GetString(), r.DocumentHash?.Value);
                }
            });
    }

    [Fact]
    public void Parse_Invalid_Query()
    {
        // assert
        Assert.Throws<InvalidGraphQLRequestException>(
            () =>
            {
                // arrange
                var source = Encoding.UTF8.GetBytes("{\"query\":\"\"}".NormalizeLineBreaks());
                var parserOptions = new ParserOptions();
                var requestParser = new Utf8GraphQLRequestParser(
                    parserOptions,
                    new DocumentCache(),
                    new Sha256DocumentHashProvider());

                // act
                requestParser.Parse(source);
            });
    }

    [Fact]
    public void Parse_Empty_OperationName()
    {
        // arrange
        var source = Encoding.UTF8.GetBytes(
            """
            {
                "operationName": "",
                "query": "{}"
            }
            """.NormalizeLineBreaks());
        var parserOptions = new ParserOptions();
        var requestParser = new Utf8GraphQLRequestParser(
            parserOptions,
            new DocumentCache(),
            new Sha256DocumentHashProvider());

        // act
        var batch = requestParser.Parse(source);

        // assert
        var request = Assert.Single(batch);
        Assert.Null(request.OperationName);
    }

    [Fact]
    public void Parse_Empty_Json()
    {
        // assert
        Assert.Throws<InvalidGraphQLRequestException>(
            () =>
            {
                // arrange
                var source = Encoding.UTF8.GetBytes("{ }"
                .NormalizeLineBreaks());
                var parserOptions = new ParserOptions();
                var requestParser = new Utf8GraphQLRequestParser(
                    parserOptions,
                    new DocumentCache(),
                    new Sha256DocumentHashProvider());

                // act
                requestParser.Parse(source);
            });
    }

    [Fact]
    public void Parse_Empty_String()
    {
        // assert
        Assert.Throws<InvalidGraphQLRequestException>(
            () =>
            {
                // arrange
                var source = Encoding.UTF8.GetBytes(string.Empty);
                var parserOptions = new ParserOptions();
                var requestParser = new Utf8GraphQLRequestParser(
                    parserOptions,
                    new DocumentCache(),
                    new Sha256DocumentHashProvider());

                // act
                requestParser.Parse(source);
            });
    }

    [Fact]
    public void Parse_Space_String()
    {
        // assert
        Assert.Throws<InvalidGraphQLRequestException>(
            () =>
            {
                // arrange
                var source = " "u8.ToArray();
                var parserOptions = new ParserOptions();
                var requestParser = new Utf8GraphQLRequestParser(
                    parserOptions,
                    new DocumentCache(),
                    new Sha256DocumentHashProvider());

                // act
                requestParser.Parse(source);
            });
    }

    [Fact]
    public void Parse_Batch_Empty()
    {
        // arrange
        var source = "[]"u8.ToArray();

        // act
        var batch = Utf8GraphQLRequestParser.Parse(source);

        // assert
        Assert.Empty(batch);
    }

    [Fact]
    public void Parse_Batch_Single_Request()
    {
        // arrange
        var source = Encoding.UTF8.GetBytes(
            JsonConvert.SerializeObject(
                new[]
                {
                    new GraphQLRequestDto(query: "{ __typename }")
                }));

        // act
        var batch = Utf8GraphQLRequestParser.Parse(source);

        // assert
        Assert.Single(batch);
        Assert.NotNull(batch[0].Document);
    }

    [Fact]
    public void Parse_Batch_Multiple_Requests()
    {
        // arrange
        var source = Encoding.UTF8.GetBytes(
            JsonConvert.SerializeObject(
                new[]
                {
                    new GraphQLRequestDto(query: "{ __typename }", operationName: "A"),
                    new GraphQLRequestDto(query: "{ __schema { queryType { name } } }", operationName: "B"),
                    new GraphQLRequestDto(query: "{ __type(name: \"Query\") { name } }", operationName: "C")
                }));

        // act
        var batch = Utf8GraphQLRequestParser.Parse(source);

        // assert
        Assert.Equal(3, batch.Length);
        Assert.Equal("A", batch[0].OperationName);
        Assert.Equal("B", batch[1].OperationName);
        Assert.Equal("C", batch[2].OperationName);
        Assert.All(batch, r => Assert.NotNull(r.Document));
    }

    [Fact]
    public void Parse_Batch_Large_Requires_Array_Expansion()
    {
        // arrange - create more than 16 requests to test array expansion
        var requests = new GraphQLRequestDto[20];
        for (var i = 0; i < 20; i++)
        {
            requests[i] = new GraphQLRequestDto(query: "{ __typename }", operationName: $"Op{i}");
        }
        var source = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(requests));

        // act
        var batch = Utf8GraphQLRequestParser.Parse(source);

        // assert
        Assert.Equal(20, batch.Length);
        for (var i = 0; i < 20; i++)
        {
            Assert.Equal($"Op{i}", batch[i].OperationName);
            Assert.NotNull(batch[i].Document);
        }
    }

    [Fact]
    public void Parse_Query_With_Newline_Escape()
    {
        // arrange
        const string query = "query { user(name: \"John\\nDoe\") { id } }";
        var source = Encoding.UTF8.GetBytes(
            JsonConvert.SerializeObject(new GraphQLRequestDto(query: query)));

        // act
        var batch = Utf8GraphQLRequestParser.Parse(source);

        // assert
        var request = Assert.Single(batch);
        Assert.NotNull(request.Document);
        // Verify the document was parsed correctly - formatted GraphQL will have escaped newline
        Assert.Contains("John\\nDoe", request.Document.ToString());
    }

    [Fact]
    public void Parse_Query_With_Tab_Escape()
    {
        // arrange
        const string query = "query { user(name: \"John\\tDoe\") { id } }";
        var source = Encoding.UTF8.GetBytes(
            JsonConvert.SerializeObject(new GraphQLRequestDto(query: query)));

        // act
        var batch = Utf8GraphQLRequestParser.Parse(source);

        // assert
        var request = Assert.Single(batch);
        Assert.NotNull(request.Document);
        // Formatted GraphQL will have escaped tab
        Assert.Contains("John\\tDoe", request.Document.ToString());
    }

    [Fact]
    public void Parse_Query_With_Quote_Escape()
    {
        // arrange
        const string query = "query { user(name: \"John \\\"The Boss\\\" Doe\") { id } }";
        var source = Encoding.UTF8.GetBytes(
            JsonConvert.SerializeObject(new GraphQLRequestDto(query: query)));

        // act
        var batch = Utf8GraphQLRequestParser.Parse(source);

        // assert
        var request = Assert.Single(batch);
        Assert.NotNull(request.Document);
        // ReSharper disable once GrammarMistakeInComment
        // The formatted GraphQL will have escaped quotes: \"
        Assert.Contains("John \\\"The Boss\\\" Doe", request.Document.ToString());
    }

    [Fact]
    public void Parse_Query_With_Backslash_Escape()
    {
        // arrange
        const string query = "query { user(path: \"C:\\\\Users\\\\Admin\") { id } }";
        var source = Encoding.UTF8.GetBytes(
            JsonConvert.SerializeObject(new GraphQLRequestDto(query: query)));

        // act
        var batch = Utf8GraphQLRequestParser.Parse(source);

        // assert
        var request = Assert.Single(batch);
        Assert.NotNull(request.Document);
        // The formatted GraphQL will have escaped backslashes: \\
        Assert.Contains("C:\\\\Users\\\\Admin", request.Document.ToString());
    }

    [Fact]
    public void Parse_Query_With_Unicode_Escape()
    {
        // arrange
        const string query = "query { user(name: \"\\u0043\\u006C\\u0061\\u0075\\u0064\\u0065\") { id } }";
        var source = Encoding.UTF8.GetBytes(
            JsonConvert.SerializeObject(new GraphQLRequestDto(query: query)));

        // act
        var batch = Utf8GraphQLRequestParser.Parse(source);

        // assert
        var request = Assert.Single(batch);
        Assert.NotNull(request.Document);
        // \u0043\u006C\u0061\u0075\u0064\u0065 = "Claude"
        Assert.Contains("Claude", request.Document.ToString());
    }

    [Fact]
    public void Parse_Query_With_Mixed_Escapes()
    {
        // arrange
        const string query = "query { user(data: \"Line1\\nTab\\tQuote\\\"Backslash\\\\Unicode\\u0041\") { id } }";
        var source = Encoding.UTF8.GetBytes(
            JsonConvert.SerializeObject(new GraphQLRequestDto(query: query)));

        // act
        var batch = Utf8GraphQLRequestParser.Parse(source);

        // assert
        var request = Assert.Single(batch);
        Assert.NotNull(request.Document);
        var docString = request.Document.ToString();
        // The formatted GraphQL will have properly escaped special characters
        Assert.Contains("Line1\\n", docString);  // \n becomes \\n in GraphQL syntax
        Assert.Contains("Tab\\t", docString);     // \t becomes \\t in GraphQL syntax
        Assert.Contains("Quote\\\"", docString);  // \" becomes \\\" in GraphQL syntax
        Assert.Contains("Backslash\\\\", docString);  // \\ becomes \\\\ in GraphQL syntax
        Assert.Contains("UnicodeA", docString); // \u0041 = 'A' - actual character
    }

    [Fact]
    public void Parse_Socket_Message_Missing_Type()
    {
        // arrange
        var source = Encoding.UTF8.GetBytes(
            JsonConvert.SerializeObject(
                new Dictionary<string, object>
                {
                    { "id", "123" },
                    { "payload", new Dictionary<string, object> { { "query", "{ __typename }" } } }
                }));

        // act
        var message = Utf8GraphQLSocketMessageParser.ParseMessage(source);

        // assert
        Assert.Null(message.Type);
        Assert.Equal("123", message.Id);
        Assert.False(message.Payload.IsEmpty);
    }

    [Fact]
    public void Parse_Socket_Message_Missing_Id()
    {
        // arrange
        var source = Encoding.UTF8.GetBytes(
            JsonConvert.SerializeObject(
                new Dictionary<string, object>
                {
                    { "type", "subscribe" },
                    { "payload", new Dictionary<string, object> { { "query", "{ __typename }" } } }
                }));

        // act
        var message = Utf8GraphQLSocketMessageParser.ParseMessage(source);

        // assert
        Assert.Equal("subscribe", message.Type);
        Assert.Null(message.Id);
        Assert.False(message.Payload.IsEmpty);
    }

    [Fact]
    public void Parse_Socket_Message_Missing_Payload()
    {
        // arrange
        var source = Encoding.UTF8.GetBytes(
            JsonConvert.SerializeObject(
                new Dictionary<string, object>
                {
                    { "type", "connection_ack" },
                    { "id", "456" }
                }));

        // act
        var message = Utf8GraphQLSocketMessageParser.ParseMessage(source);

        // assert
        Assert.Equal("connection_ack", message.Type);
        Assert.Equal("456", message.Id);
        Assert.True(message.Payload.IsEmpty);
    }

    [Fact]
    public void Parse_Socket_Message_Null_Type()
    {
        // arrange
        var source = Encoding.UTF8.GetBytes(
            JsonConvert.SerializeObject(
                new Dictionary<string, object?>
                {
                    { "type", null },
                    { "id", "789" },
                    { "payload", new Dictionary<string, object> { { "query", "{ __typename }" } } }
                }));

        // act
        var message = Utf8GraphQLSocketMessageParser.ParseMessage(source);

        // assert
        Assert.Null(message.Type);
        Assert.Equal("789", message.Id);
    }

    [Fact]
    public void Parse_Socket_Message_Null_Id()
    {
        // arrange
        var source = Encoding.UTF8.GetBytes(
            JsonConvert.SerializeObject(
                new Dictionary<string, object?>
                {
                    { "type", "ping" },
                    { "id", null },
                    { "payload", new Dictionary<string, object> { { "query", "{ __typename }" } } }
                }));

        // act
        var message = Utf8GraphQLSocketMessageParser.ParseMessage(source);

        // assert
        Assert.Equal("ping", message.Type);
        Assert.Null(message.Id);
    }

    [Fact]
    public void Parse_Socket_Message_Empty_Payload_Object()
    {
        // arrange
        var source = Encoding.UTF8.GetBytes(
            JsonConvert.SerializeObject(
                new Dictionary<string, object>
                {
                    { "type", "complete" },
                    { "id", "abc" },
                    { "payload", new Dictionary<string, object>() }
                }));

        // act
        var message = Utf8GraphQLSocketMessageParser.ParseMessage(source);

        // assert
        Assert.Equal("complete", message.Type);
        Assert.Equal("abc", message.Id);
        Assert.False(message.Payload.IsEmpty);
    }

    [Fact]
    public void Parse_Query_Invalid_Type_Number_Throws()
    {
        // arrange
        var source = "{\"query\": 123}"u8.ToArray();

        // act & assert
        var exception = Assert.Throws<InvalidGraphQLRequestException>(
            () => Utf8GraphQLRequestParser.Parse(source));
        Assert.Contains("query", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_Query_Invalid_Type_Object_Throws()
    {
        // arrange
        var source = "{\"query\": {}}"u8.ToArray();

        // act & assert
        var exception = Assert.Throws<InvalidGraphQLRequestException>(
            () => Utf8GraphQLRequestParser.Parse(source));
        Assert.Contains("query", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_Query_Invalid_Type_Array_Throws()
    {
        // arrange
        var source = "{\"query\": []}"u8.ToArray();

        // act & assert
        var exception = Assert.Throws<InvalidGraphQLRequestException>(
            () => Utf8GraphQLRequestParser.Parse(source));
        Assert.Contains("query", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_Query_Invalid_Type_Boolean_Throws()
    {
        // arrange
        var source = "{\"query\": true}"u8.ToArray();

        // act & assert
        var exception = Assert.Throws<InvalidGraphQLRequestException>(
            () => Utf8GraphQLRequestParser.Parse(source));
        Assert.Contains("query", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_DocumentId_Invalid_Type_Number_Throws()
    {
        // arrange
        var source = "{\"id\": 456, \"query\": \"{ __typename }\"}"u8.ToArray();

        // act & assert
        var exception = Assert.Throws<InvalidGraphQLRequestException>(
            () => Utf8GraphQLRequestParser.Parse(source));
        Assert.Contains("id", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_DocumentId_Invalid_Type_Object_Throws()
    {
        // arrange
        var source = "{\"documentId\": {}, \"query\": \"{ __typename }\"}"u8.ToArray();

        // act & assert
        var exception = Assert.Throws<InvalidGraphQLRequestException>(
            () => Utf8GraphQLRequestParser.Parse(source));
        Assert.Contains("documentId", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_OperationName_Invalid_Type_Number_Throws()
    {
        // arrange
        var source = "{\"operationName\": 789, \"query\": \"{ __typename }\"}"u8.ToArray();

        // act & assert
        var exception = Assert.Throws<InvalidGraphQLRequestException>(
            () => Utf8GraphQLRequestParser.Parse(source));
        Assert.Contains("operationName", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_OperationName_Invalid_Type_Array_Throws()
    {
        // arrange
        var source = "{\"operationName\": [\"test\"], \"query\": \"{ __typename }\"}"u8.ToArray();

        // act & assert
        var exception = Assert.Throws<InvalidGraphQLRequestException>(
            () => Utf8GraphQLRequestParser.Parse(source));
        Assert.Contains("operationName", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_OnError_Invalid_Type_Number_Throws()
    {
        // arrange
        var source = "{\"onError\": 123, \"query\": \"{ __typename }\"}"u8.ToArray();

        // act & assert
        var exception = Assert.Throws<InvalidGraphQLRequestException>(
            () => Utf8GraphQLRequestParser.Parse(source));
        Assert.Contains("onError", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_OnError_Invalid_Type_Object_Throws()
    {
        // arrange
        var source = "{\"onError\": {\"mode\": \"HALT\"}, \"query\": \"{ __typename }\"}"u8.ToArray();

        // act & assert
        var exception = Assert.Throws<InvalidGraphQLRequestException>(
            () => Utf8GraphQLRequestParser.Parse(source));
        Assert.Contains("onError", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_Variables_Invalid_Type_String_Throws()
    {
        // arrange
        var source = "{\"variables\": \"invalid\", \"query\": \"{ __typename }\"}"u8.ToArray();

        // act & assert
        var exception = Assert.Throws<InvalidGraphQLRequestException>(
            () => Utf8GraphQLRequestParser.Parse(source));
        Assert.Contains("variables", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_Variables_Invalid_Type_Number_Throws()
    {
        // arrange
        var source = "{\"variables\": 123, \"query\": \"{ __typename }\"}"u8.ToArray();

        // act & assert
        var exception = Assert.Throws<InvalidGraphQLRequestException>(
            () => Utf8GraphQLRequestParser.Parse(source));
        Assert.Contains("variables", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_Variables_Array_Accepted()
    {
        // arrange
        var source = "{\"variables\": [1, 2, 3], \"query\": \"{ __typename }\"}"u8.ToArray();

        // act
        var result = Utf8GraphQLRequestParser.Parse(source);

        // assert
        var request = Assert.Single(result);
        Assert.NotNull(request.Variables);
    }

    [Fact]
    public void Parse_Extensions_Invalid_Type_String_Throws()
    {
        // arrange
        var source = "{\"extensions\": \"invalid\", \"query\": \"{ __typename }\"}"u8.ToArray();

        // act & assert
        var exception = Assert.Throws<InvalidGraphQLRequestException>(
            () => Utf8GraphQLRequestParser.Parse(source));
        Assert.Contains("extensions", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_Extensions_Invalid_Type_Array_Throws()
    {
        // arrange
        var source = "{\"extensions\": [], \"query\": \"{ __typename }\"}"u8.ToArray();

        // act & assert
        var exception = Assert.Throws<InvalidGraphQLRequestException>(
            () => Utf8GraphQLRequestParser.Parse(source));
        Assert.Contains("extensions", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_Extensions_Invalid_Type_Number_Throws()
    {
        // arrange
        var source = "{\"extensions\": 456, \"query\": \"{ __typename }\"}"u8.ToArray();

        // act & assert
        var exception = Assert.Throws<InvalidGraphQLRequestException>(
            () => Utf8GraphQLRequestParser.Parse(source));
        Assert.Contains("extensions", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_Invalid_Request_Structure_Number_Throws()
    {
        // arrange
        var source = "123"u8.ToArray();

        // act & assert
        var exception = Assert.Throws<InvalidGraphQLRequestException>(
            () => Utf8GraphQLRequestParser.Parse(source));
        Assert.Contains("Invalid request structure", exception.Message);
    }

    [Fact]
    public void Parse_Invalid_Request_Structure_String_Throws()
    {
        // arrange
        var source = "\"test\""u8.ToArray();

        // act & assert
        var exception = Assert.Throws<InvalidGraphQLRequestException>(
            () => Utf8GraphQLRequestParser.Parse(source));
        Assert.Contains("Invalid request structure", exception.Message);
    }

    [Fact]
    public void Parse_Query_Null_Accepted()
    {
        // arrange
        var source = "{\"query\": null, \"id\": \"abc\"}"u8.ToArray();

        // act
        var result = Utf8GraphQLRequestParser.Parse(source);

        // assert
        var request = Assert.Single(result);
        Assert.Null(request.Document);
        Assert.Equal("abc", request.DocumentId?.Value);
    }

    [Fact]
    public void Parse_Variables_Null_Accepted()
    {
        // arrange
        var source = "{\"variables\": null, \"query\": \"{ __typename }\"}"u8.ToArray();

        // act
        var result = Utf8GraphQLRequestParser.Parse(source);

        // assert
        var request = Assert.Single(result);
        Assert.Null(request.Variables);
    }

    [Fact]
    public void Parse_Extensions_Null_Accepted()
    {
        // arrange
        var source = "{\"extensions\": null, \"query\": \"{ __typename }\"}"u8.ToArray();

        // act
        var result = Utf8GraphQLRequestParser.Parse(source);

        // assert
        var request = Assert.Single(result);
        Assert.Null(request.Extensions);
    }

    [Fact]
    public void Parse_Multiple_Invalid_Properties_Throws_On_First()
    {
        // arrange
        var source = "{\"query\": 123, \"variables\": \"invalid\", \"extensions\": []}"u8.ToArray();

        // act & assert
        // Should throw on the first invalid property encountered (query)
        var exception = Assert.Throws<InvalidGraphQLRequestException>(
            () => Utf8GraphQLRequestParser.Parse(source));
        Assert.Contains("query", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private class GraphQLRequestDto(
        string query,
        string? id = null,
        string? operationName = null,
        string? onError = null,
        IReadOnlyDictionary<string, object>? variables = null,
        IReadOnlyDictionary<string, object>? extensions = null)
    {
        [JsonProperty("operationName")]
        public string? OperationName { get; set; } = operationName;

        [JsonProperty("id")]
        public string? Id { get; set; } = id;

        [JsonProperty("query")]
        public string Query { get; set; } = query;

        [JsonProperty("onError")]
        public string? OnError { get; set; } = onError;

        [JsonProperty("variables")]
        public IReadOnlyDictionary<string, object>? Variables { get; set; } = variables;

        [JsonProperty("extensions")]
        public IReadOnlyDictionary<string, object>? Extensions { get; set; } = extensions;
    }

    private sealed class CustomGraphQLRequestDto(string customProperty, string query)
        : GraphQLRequestDto(query)
    {
        public string CustomProperty { get; set; } = customProperty;
    }

    private sealed class RelayGraphQLRequestDto(string id, string query)
        : GraphQLRequestDto(query)
    {
        [JsonProperty("id")]
        public new string Id { get; set; } = id;
    }

    private sealed class DocumentCache : IDocumentCache
    {
        private readonly Dictionary<string, CachedDocument> _cache = [];

        public int Capacity => int.MaxValue;

        public int Count => _cache.Count;

        public void TryAddDocument(string documentId, CachedDocument document)
        {
            _cache.TryAdd(documentId, document);
        }

        public bool TryGetDocument(
            string documentId,
            [NotNullWhen(true)] out CachedDocument? document) =>
            _cache.TryGetValue(documentId, out document);

        public void Clear()
        {
            _cache.Clear();
        }
    }
}
