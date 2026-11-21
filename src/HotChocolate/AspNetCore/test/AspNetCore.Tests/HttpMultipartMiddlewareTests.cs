using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Net;
using System.Text.Json;
using HotChocolate.AspNetCore.Instrumentation;
using HotChocolate.AspNetCore.Tests.Utilities;
using HotChocolate.Types;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;

namespace HotChocolate.AspNetCore;

public class HttpMultipartMiddlewareTests(TestServerFactory serverFactory) : ServerTestBase(serverFactory)
{
    [Fact]
    public async Task EmptyForm_Test()
    {
        // arrange
        var server = CreateStarWarsServer();

        // act
        var form = new MultipartFormDataContent();
        form.Headers.Add(HttpHeaderKeys.Preflight, "1");
        var result = await server.PostMultipartAsync(form);

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Fail_Without_Preflight_Header()
    {
        // arrange
        var server = CreateStarWarsServer();

        // act
        var form = new MultipartFormDataContent();
        var result = await server.PostMultipartAsync(form);

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Preflight_Should_ReturnBadRequest_When_EveryAcceptMediaTypeIsRejected()
    {
        // arrange
        var server = CreateStarWarsServer();
        var client = server.CreateClient();

        // act
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            new Uri("http://localhost:5000/graphql"))
        {
            Content = new MultipartFormDataContent()
        };
        request.Headers.Add(HeaderNames.Accept, "application/graphql-response+json;q=0");

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task EmptyOperations_Test()
    {
        // arrange
        var server = CreateStarWarsServer();

        // act
        var form = new MultipartFormDataContent
        {
            { new StringContent(""), "operations" }
        };

        form.Headers.Add(HttpHeaderKeys.Preflight, "1");

        var result = await server.PostMultipartAsync(form);

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task IncompleteOperations_Test()
    {
        // arrange
        var server = CreateStarWarsServer();

        // act
        var form = new MultipartFormDataContent
        {
            { new StringContent("{}"), "operations" },
            { new StringContent("{}"), "map" }
        };

        form.Headers.Add(HttpHeaderKeys.Preflight, "1");

        var result = await server.PostMultipartAsync(form);

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task MapWithNoOperations_Test()
    {
        // arrange
        var server = CreateStarWarsServer();

        // act
        var form = new MultipartFormDataContent
            {
                { new StringContent("{}"), "map" }
            };

        form.Headers.Add(HttpHeaderKeys.Preflight, "1");

        var result = await server.PostMultipartAsync(form);

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task MapBeforeOperations_Test()
    {
        // arrange
        var server = CreateStarWarsServer();

        // act
        var form = new MultipartFormDataContent
            {
                { new StringContent("{}"), "map" },
                { new StringContent("{}"), "operations" }
            };

        form.Headers.Add(HttpHeaderKeys.Preflight, "1");

        var result = await server.PostMultipartAsync(form);

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task EmptyMap_Test()
    {
        // arrange
        var server = CreateStarWarsServer();

        // act
        var form = new MultipartFormDataContent
            {
                { new StringContent("{}"), "operations" },
                { new StringContent(""), "map" }
            };

        form.Headers.Add(HttpHeaderKeys.Preflight, "1");

        var result = await server.PostMultipartAsync(form);

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task InvalidMap_Test()
    {
        // arrange
        var server = CreateStarWarsServer();

        // act
        var form = new MultipartFormDataContent
            {
                { new StringContent("{}"), "operations" },
                { new StringContent("data"), "map" }
            };

        form.Headers.Add(HttpHeaderKeys.Preflight, "1");

        var result = await server.PostMultipartAsync(form);

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task MissingFile_Test()
    {
        // arrange
        var server = CreateStarWarsServer();

        // act
        var form = new MultipartFormDataContent
            {
                { new StringContent("{}"), "operations" },
                { new StringContent("{ \"1\": [\"variables.file\"] }"), "map" }
            };

        form.Headers.Add(HttpHeaderKeys.Preflight, "1");

        var result = await server.PostMultipartAsync(form);

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task MissingKeyInMap_Test()
    {
        // arrange
        var server = CreateStarWarsServer();

        // act
        var form = new MultipartFormDataContent
            {
                { new StringContent("{}"), "operations" },
                { new StringContent("{ \"\": [\"variables.file\"] }"), "map" }
            };

        form.Headers.Add(HttpHeaderKeys.Preflight, "1");

        var result = await server.PostMultipartAsync(form);

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task MissingObjectPathsForKey_Test()
    {
        // arrange
        var server = CreateStarWarsServer();

        // act
        var form = new MultipartFormDataContent
            {
                { new StringContent("{}"), "operations" },
                { new StringContent("{ \"1\": [] }"), "map" }
            };

        form.Headers.Add(HttpHeaderKeys.Preflight, "1");

        var result = await server.PostMultipartAsync(form);

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Upload_File()
    {
        // arrange
        var server = CreateStarWarsServer();

        const string query = @"
                query ($upload: Upload!) {
                    singleUpload(file: $upload)
                }";

        var request = JsonConvert.SerializeObject(
            new ClientQueryRequest
            {
                Query = query,
                Variables = new Dictionary<string, object?>
                {
                    { "upload", null }
                }
            });

        // act
        var form = new MultipartFormDataContent
            {
                { new StringContent(request), "operations" },
                { new StringContent("{ \"1\": [\"variables.upload\"] }"), "map" },
                { new StringContent("abc"), "1", "foo.bar" }
            };

        form.Headers.Add(HttpHeaderKeys.Preflight, "1");

        var result = await server.PostMultipartAsync(form, path: "/upload");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Upload_Optional_File()
    {
        // arrange
        var server = CreateStarWarsServer();

        const string query = @"
                query ($upload: Upload) {
                    optionalUpload(file: $upload)
                }";

        var request = JsonConvert.SerializeObject(
            new ClientQueryRequest
            {
                Query = query,
                Variables = new Dictionary<string, object?>
                {
                    { "upload", null }
                }
            });

        // act
        var form = new MultipartFormDataContent
            {
                { new StringContent(request), "operations" },
                { new StringContent("{ \"1\": [\"variables.upload\"] }"), "map" },
                { new StringContent("abc"), "1", "foo.bar" }
            };

        form.Headers.Add(HttpHeaderKeys.Preflight, "1");

        var result = await server.PostMultipartAsync(form, path: "/upload");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Upload_Optional_File_Not_Provided()
    {
        // arrange
        var server = CreateStarWarsServer();

        // act
        var result = await server.PostAsync(
            new ClientQueryRequest
            {
                Query = @"
                    query ($upload: Upload) {
                        optionalUpload(file: $upload)
                    }",
                Variables = new Dictionary<string, object?>
                {
                    { "upload", null }
                }
            },
            "/upload");

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "ContentType": "application/graphql-response+json; charset=utf-8",
              "StatusCode": "OK",
              "Data": {
                "optionalUpload": null
              },
              "Errors": null,
              "Extensions": null
            }
            """);
    }

    [Fact]
    public async Task Upload_Should_Not_DoubleEncode_When_StringVariableIsEscaped()
    {
        // arrange
        var server = CreateStarWarsServer();

        const string query = @"
                query ($upload: Upload!, $text: String!) {
                    uploadWithText(file: $upload, text: $text)
                }";

        var request = JsonConvert.SerializeObject(
            new ClientQueryRequest
            {
                Query = query,
                Variables = new Dictionary<string, object?>
                {
                    { "upload", null },
                    { "text", "test \" test" }
                }
            });

        // act
        var form = new MultipartFormDataContent
        {
            { new StringContent(request), "operations" },
            { new StringContent("{ \"1\": [\"variables.upload\"] }"), "map" },
            { new StringContent("abc"), "1", "foo.bar" }
        };

        form.Headers.Add(HttpHeaderKeys.Preflight, "1");

        var result = await server.PostMultipartAsync(form, path: "/upload");

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "ContentType": "application/graphql-response+json; charset=utf-8",
              "StatusCode": "OK",
              "Data": {
                "uploadWithText": "test \" test"
              },
              "Errors": null,
              "Extensions": null
            }
            """);
    }

    [Fact]
    public async Task Upload_Should_Not_DoubleEncode_When_StringVariableHasMixedEscapes()
    {
        // arrange
        var server = CreateStarWarsServer();

        const string query = @"
                query ($upload: Upload!, $text: String!) {
                    uploadWithText(file: $upload, text: $text)
                }";

        var request = JsonConvert.SerializeObject(
            new ClientQueryRequest
            {
                Query = query,
                Variables = new Dictionary<string, object?>
                {
                    { "upload", null },
                    { "text", "q\" b\\ n\n ué" }
                }
            });

        // act
        var form = new MultipartFormDataContent
        {
            { new StringContent(request), "operations" },
            { new StringContent("{ \"1\": [\"variables.upload\"] }"), "map" },
            { new StringContent("abc"), "1", "foo.bar" }
        };

        form.Headers.Add(HttpHeaderKeys.Preflight, "1");

        var result = await server.PostMultipartAsync(form, path: "/upload");

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "ContentType": "application/graphql-response+json; charset=utf-8",
              "StatusCode": "OK",
              "Data": {
                "uploadWithText": "q\" b\\ n\n ué"
              },
              "Errors": null,
              "Extensions": null
            }
            """);
    }

    [Fact]
    public async Task Upload_Nullable_File()
    {
        // arrange
        var server = CreateStarWarsServer();

        const string query = @"
                query ($upload: Upload) {
                    nullableUpload(file: $upload)
                }";

        var request = JsonConvert.SerializeObject(
            new ClientQueryRequest
            {
                Query = query,
                Variables = new Dictionary<string, object?>
                {
                    { "upload", null }
                }
            });

        // act
        var form = new MultipartFormDataContent
        {
            { new StringContent(request), "operations" },
            { new StringContent("{ \"1\": [\"variables.upload\"] }"), "map" },
            { new StringContent("abc"), "1", "foo.bar" }
        };

        form.Headers.Add(HttpHeaderKeys.Preflight, "1");

        var result = await server.PostMultipartAsync(form, path: "/upload");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Upload_Nullable_File_Not_Provided()
    {
        // arrange
        var server = CreateStarWarsServer();

        // act
        var result = await server.PostAsync(
            new ClientQueryRequest
            {
                Query = @"
                    query ($upload: Upload) {
                        nullableUpload(file: $upload)
                    }",
                Variables = new Dictionary<string, object?>
                {
                    { "upload", null }
                }
            },
            "/upload");

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "ContentType": "application/graphql-response+json; charset=utf-8",
              "StatusCode": "OK",
              "Data": {
                "nullableUpload": null
              },
              "Errors": null,
              "Extensions": null
            }
            """);
    }

    [Fact]
    public async Task Upload_Optional_File_In_InputObject()
    {
        // arrange
        var server = CreateStarWarsServer();

        const string query = @"
                query ($input: InputWithOptionalFileInput!) {
                    optionalObjectUpload(input: $input)
                }";

        var request = JsonConvert.SerializeObject(
            new ClientQueryRequest
            {
                Query = query,
                Variables = new Dictionary<string, object?>
                {
                    { "input", new Dictionary<string, object?> { { "file", null } } }
                }
            });

        // act
        var form = new MultipartFormDataContent
            {
                { new StringContent(request), "operations" },
                { new StringContent("{ \"1\": [\"variables.input.file\"] }"), "map" },
                { new StringContent("abc"), "1", "foo.bar" }
            };

        form.Headers.Add(HttpHeaderKeys.Preflight, "1");

        var result = await server.PostMultipartAsync(form, path: "/upload");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Upload_Optional_File_In_Inline_InputObject()
    {
        // arrange
        var server = CreateStarWarsServer();

        const string query = @"
                query ($upload: Upload!) {
                    optionalObjectUpload(input: { file: $upload })
                }";

        var request = JsonConvert.SerializeObject(
            new ClientQueryRequest
            {
                Query = query,
                Variables = new Dictionary<string, object?>
                {
                    { "upload", null }
                }
            });

        // act
        var form = new MultipartFormDataContent
            {
                { new StringContent(request), "operations" },
                { new StringContent("{ \"1\": [\"variables.upload\"] }"), "map" },
                { new StringContent("abc"), "1", "foo.bar" }
            };

        form.Headers.Add(HttpHeaderKeys.Preflight, "1");

        var result = await server.PostMultipartAsync(form, path: "/upload");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Upload_File_In_InputObject()
    {
        // arrange
        var server = CreateStarWarsServer();

        const string query = @"
                query ($input: InputWithFileInput!) {
                    objectUpload(input: $input)
                }";

        var request = JsonConvert.SerializeObject(
            new ClientQueryRequest
            {
                Query = query,
                Variables = new Dictionary<string, object?>
                {
                    { "input", new Dictionary<string, object?> { { "file", null } } }
                }
            });

        // act
        var form = new MultipartFormDataContent
            {
                { new StringContent(request), "operations" },
                { new StringContent("{ \"1\": [\"variables.input.file\"] }"), "map" },
                { new StringContent("abc"), "1", "foo.bar" }
            };

        form.Headers.Add(HttpHeaderKeys.Preflight, "1");

        var result = await server.PostMultipartAsync(form, path: "/upload");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Upload_File_In_InputObject_With_NonNull_Placeholder_Should_Be_Rejected()
    {
        // arrange
        var server = CreateStarWarsServer();

        const string query = @"
                query ($input: InputWithFileInput!) {
                    objectUpload(input: $input)
                }";

        var request = JsonConvert.SerializeObject(
            new ClientQueryRequest
            {
                Query = query,
                Variables = new Dictionary<string, object?>
                {
                    {
                        "input",
                        new Dictionary<string, object?>
                        {
                            { "file", new Dictionary<string, object?> { { "path", "./foo.bar" } } }
                        }
                    }
                }
            });

        // act
        var form = new MultipartFormDataContent
            {
                { new StringContent(request), "operations" },
                { new StringContent("{ \"1\": [\"variables.input.file\"] }"), "map" },
                { new StringContent("abc"), "1", "foo.bar" }
            };

        form.Headers.Add(HttpHeaderKeys.Preflight, "1");

        var result = await server.PostMultipartAsync(form, path: "/upload");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Upload_File_Inline_InputObject()
    {
        // arrange
        var server = CreateStarWarsServer();

        const string query = @"
            query ($upload: Upload!) {
                objectUpload(input: { file: $upload })
            }";

        var request = JsonConvert.SerializeObject(
            new ClientQueryRequest
            {
                Query = query,
                Variables = new Dictionary<string, object?>
                {
                    { "upload", null }
                }
            });

        // act
        var form = new MultipartFormDataContent
            {
                { new StringContent(request), "operations" },
                { new StringContent("{ \"1\": [\"variables.upload\"] }"), "map" },
                { new StringContent("abc"), "1", "foo.bar" }
            };

        form.Headers.Add(HttpHeaderKeys.Preflight, "1");

        var result = await server.PostMultipartAsync(form, path: "/upload");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Upload_File_In_List()
    {
        // arrange
        var server = CreateStarWarsServer();

        const string query =
            @"query ($input: [[InputWithFileInput!]!]!) {
                listUpload(input: $input)
            }";

        var request = JsonConvert.SerializeObject(
            new ClientQueryRequest
            {
                Query = query,
                Variables = new Dictionary<string, object?>
                {
                    {
                        "input",
                        new List<object>
                        {
                            new List<object>
                            {
                                new Dictionary<string, object?> { { "file", null } }
                            }
                        }
                    }
                }
            });

        // act
        var form = new MultipartFormDataContent
            {
                { new StringContent(request), "operations" },
                { new StringContent("{ \"1\": [\"variables.input.0.0.file\"] }"), "map" },
                { new StringContent("abc"), "1", "foo.bar" }
            };

        form.Headers.Add(HttpHeaderKeys.Preflight, "1");

        var result = await server.PostMultipartAsync(form, path: "/upload");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Upload_Too_Large_File_Test()
    {
        // arrange
        var server = CreateStarWarsServer();

        const string query = @"
                query ($upload: Upload!) {
                    singleUpload(file: $upload)
                }";

        var request = JsonConvert.SerializeObject(
            new ClientQueryRequest
            {
                Query = query,
                Variables = new Dictionary<string, object?>
                {
                    { "upload", null }
                }
            });

        const int count = 1024 * 1024 * 129;
        var buffer = new byte[count];

        for (var i = 0; i < buffer.Length; i++)
        {
            buffer[i] = 0xFF;
        }

        // act
        var form = new MultipartFormDataContent
            {
                { new StringContent(request), "operations" },
                { new StringContent("{ \"1\": [\"variables.upload\"] }"), "map" },
                { new ByteArrayContent(buffer), "1", "foo.bar" }
            };

        form.Headers.Add(HttpHeaderKeys.Preflight, "1");

        var result = await server.PostMultipartAsync(form, path: "/upload");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Post_Should_ReturnBadRequest_When_AntiforgeryValidationFailed()
    {
        // arrange
        var server = ServerFactory.Create(
            services => services
                .AddRouting()
                .AddGraphQLServer()
                .AddQueryType(d => d.Field("greeting").Type<StringType>().Resolve("Hello")),
            app => app
                .Use(
                    next => context =>
                    {
                        context.Features.Set<IAntiforgeryValidationFeature>(
                            new FailedAntiforgeryValidationFeature());
                        return next(context);
                    })
                .UseRouting()
                .UseEndpoints(endpoints => endpoints.MapGraphQL()));
        var client = server.CreateClient();

        // act
        using var form = new MultipartFormDataContent
        {
            { new StringContent("""{ "query": "{ greeting }" }"""), "operations" },
            { new StringContent("{}"), "map" }
        };
        form.Headers.Add(HttpHeaderKeys.Preflight, "1");

        using var response = await client.PostAsync(
            "/graphql",
            form,
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var body = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        body.MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "The multipart form could not be read.",
                  "extensions": {
                    "code": "HC0033",
                    "underlyingError": "This form is being accessed with a failed antiforgery validation. Validate the `IAntiforgeryValidationFeature` on the request before reading from the form."
                  }
                }
              ]
            }
            """);
    }

    [Fact]
    public async Task Upload_Should_RefuseQuery_When_QueriesOverPostAreOff()
    {
        // arrange
        var server = CreateStarWarsServer(
            configureServices: s => s
                .AddGraphQLServer("upload")
                .ModifyServerOptions(o => o.EnableQueriesOverPost = false));

        const string query =
            """
            query ($upload: Upload!) {
                singleUpload(file: $upload)
            }
            """;

        var request = JsonConvert.SerializeObject(
            new ClientQueryRequest
            {
                Query = query,
                Variables = new Dictionary<string, object?>
                {
                    { "upload", null }
                }
            });

        // act
        var form = new MultipartFormDataContent
        {
            { new StringContent(request), "operations" },
            { new StringContent("{ \"1\": [\"variables.upload\"] }"), "map" },
            { new StringContent("abc"), "1", "foo.bar" }
        };
        form.Headers.Add(HttpHeaderKeys.Preflight, "1");

        var result = await server.PostMultipartAsync(form, path: "/upload");

        // assert
        Assert.Equal(HttpStatusCode.MethodNotAllowed, result.StatusCode);
        Assert.NotNull(result.Errors);
        var error = Assert.Single(result.Errors);
        Assert.Equal("The specified operation kind is not allowed.", error["message"]);
    }

    [Fact]
    public async Task Upload_Should_BindFiles_When_BatchMapsFilesByOperationIndex()
    {
        // arrange
        var server = CreateStarWarsServer(configureServices: EnableBatchingOnUploadSchema);
        const string operations =
            """
            [
              {
                "query": "query ($file: Upload!) { singleUpload(file: $file) }",
                "variables": { "file": null }
              },
              {
                "query": "query ($i: [[InputWithFileInput!]!]!) { listUpload(input: $i) }",
                "variables": { "i": [[{ "file": null }]] }
              }
            ]
            """;
        const string map = """{ "1": ["0.variables.file"], "2": ["1.variables.i.0.0.file"] }""";
        using var form = CreateUploadForm(operations, map);

        // act
        using var response = await server.SendMultipartRequestAsync(form, "/upload");

        // assert
        Snapshot
            .Create()
            .Add(response)
            .MatchInline(
                """
                Headers:
                Vary: Accept
                Cache-Control: no-cache
                Content-Type: multipart/mixed; boundary="-"
                -------------------------->
                Status Code: OK
                -------------------------->

                ---
                Content-Type: application/json; charset=utf-8

                {"requestIndex":0,"data":{"singleUpload":"abc"}}
                ---
                Content-Type: application/json; charset=utf-8

                {"requestIndex":1,"data":{"listUpload":"def"}}
                -----

                """);
    }

    [Fact]
    public async Task Upload_Should_BindFile_When_SingleOperationBatchMapsFileByOperationIndex()
    {
        // arrange
        var server = CreateStarWarsServer(configureServices: EnableBatchingOnUploadSchema);
        const string operations =
            """
            [
              {
                "query": "query ($file: Upload!) { singleUpload(file: $file) }",
                "variables": { "file": null }
              }
            ]
            """;
        using var form = CreateUploadForm(operations, """{ "1": ["0.variables.file"] }""");

        // act
        using var response = await server.SendMultipartRequestAsync(form, "/upload");

        // assert
        Snapshot
            .Create()
            .Add(response)
            .MatchInline(
                """
                Headers:
                Vary: Accept
                Content-Type: application/graphql-response+json; charset=utf-8
                Content-Length: 31
                -------------------------->
                Status Code: OK
                -------------------------->
                {"data":{"singleUpload":"abc"}}
                """);
    }

    [Fact]
    public async Task Upload_Should_ReturnBadRequest_When_BatchMapsFileWithoutOperationIndex()
    {
        // arrange
        var server = CreateStarWarsServer(configureServices: EnableBatchingOnUploadSchema);
        const string operations =
            """
            [
              {
                "query": "query ($file: Upload!) { singleUpload(file: $file) }",
                "variables": { "file": null }
              },
              {
                "query": "query ($file: Upload!) { singleUpload(file: $file) }",
                "variables": { "file": null }
              }
            ]
            """;
        using var form = CreateUploadForm(operations, """{ "1": ["variables.file"] }""");

        // act
        using var response = await server.SendMultipartRequestAsync(form, "/upload");

        // assert
        Snapshot
            .Create()
            .Add(response)
            .MatchInline(
                """
                Headers:
                Vary: Accept
                Content-Type: application/graphql-response+json; charset=utf-8
                Content-Length: 132
                -------------------------->
                Status Code: BadRequest
                -------------------------->
                {"errors":[{"message":"The variable path must start with an operation index in a batched request.","extensions":{"code":"HC0142"}}]}
                """);
    }

    [Fact]
    public async Task Upload_Should_RefuseBatch_When_RequestBatchingIsDisabled()
    {
        // arrange
        var listener = new RecordingListener();
        var server = CreateStarWarsServer(
            configureServices: s => s
                .AddGraphQLServer("upload")
                .AddDiagnosticEventListener(_ => listener));
        const string operations =
            """
            [
              {
                "query": "query ($file: Upload!) { singleUpload(file: $file) }",
                "variables": { "file": null }
              },
              {
                "query": "query ($file: Upload!) { singleUpload(file: $file) }",
                "variables": { "file": null }
              }
            ]
            """;
        using var form = CreateUploadForm(operations, """{ "1": ["variables.file"] }""");

        // act
        using var response = await server.SendMultipartRequestAsync(form, "/upload");

        // assert
        var diagnosticError = Assert.Single(listener.Errors);
        Assert.Equal(
            "Request batching is disabled. Enable it with GraphQLServerOptions.Batching.",
            diagnosticError.Message);
        Snapshot
            .Create()
            .Add(response)
            .MatchInline(
                """
                Headers:
                Vary: Accept
                Content-Type: application/graphql-response+json; charset=utf-8
                Content-Length: 82
                -------------------------->
                Status Code: BadRequest
                -------------------------->
                {"errors":[{"message":"Invalid GraphQL Request.","extensions":{"code":"HC0009"}}]}
                """);
    }

    [Fact]
    public async Task Upload_Should_BindFile_When_SingleOperationBatchOmitsOperationIndex()
    {
        // arrange
        var server = CreateStarWarsServer(configureServices: EnableBatchingOnUploadSchema);
        const string operations =
            """
            [
              {
                "query": "query ($file: Upload!) { singleUpload(file: $file) }",
                "variables": { "file": null }
              }
            ]
            """;
        using var form = CreateUploadForm(operations, """{ "1": ["variables.file"] }""");

        // act
        using var response = await server.SendMultipartRequestAsync(form, "/upload");

        // assert
        Snapshot
            .Create()
            .Add(response)
            .MatchInline(
                """
                Headers:
                Vary: Accept
                Content-Type: application/graphql-response+json; charset=utf-8
                Content-Length: 31
                -------------------------->
                Status Code: OK
                -------------------------->
                {"data":{"singleUpload":"abc"}}
                """);
    }

    [Fact]
    public async Task Upload_Should_ReturnBadRequest_When_MapMixesOperationIndexes()
    {
        // arrange
        var server = CreateStarWarsServer(configureServices: EnableBatchingOnUploadSchema);
        const string operations =
            """
            [
              {
                "query": "query ($file: Upload!) { singleUpload(file: $file) }",
                "variables": { "file": null }
              },
              {
                "query": "query ($file: Upload!) { singleUpload(file: $file) }",
                "variables": { "file": null }
              }
            ]
            """;
        using var form = CreateUploadForm(
            operations,
            """{ "1": ["0.variables.file"], "2": ["variables.file"] }""");

        // act
        using var response = await server.SendMultipartRequestAsync(form, "/upload");

        // assert
        Snapshot
            .Create()
            .Add(response)
            .MatchInline(
                """
                Headers:
                Vary: Accept
                Content-Type: application/graphql-response+json; charset=utf-8
                Content-Length: 153
                -------------------------->
                Status Code: BadRequest
                -------------------------->
                {"errors":[{"message":"The variable paths must either all start with an operation index or all start with `variables`.","extensions":{"code":"HC0141"}}]}
                """);
    }

    [Theory]
    [InlineData(
        """
        [
          { "query": "{ __typename }" },
          { "query": "query ($f: Upload!) { singleUpload(file: $f) }", "variables": { "f": null } }
        ]
        """,
        """{ "1": ["0.variables.f"] }""")]
    [InlineData(
        """
        [
          { "query": "query ($f: Upload!) { singleUpload(file: $f) }", "variables": { "f": null } },
          { "query": "query ($f: Upload!) { singleUpload(file: $f) }", "variables": { "f": null } }
        ]
        """,
        """{ "1": ["variables.f"] }""")]
    public async Task Upload_Should_DisposeEveryRequest_When_MultipartRequestIsRejected(
        string operations,
        string map)
    {
        // arrange
        TaskCompletionSource? responseCompleted = null;
        var server = ServerFactory.Create(
            services => services
                .AddRouting()
                .AddGraphQLServer()
                .AddQueryType<UploadQuery>()
                .ModifyServerOptions(o => o.Batching = AllowedBatching.RequestBatching),
            app => app
                .Use(async (context, next) =>
                {
                    // completion callbacks run in reverse order, so this one runs after the
                    // callbacks that dispose the requests
                    var completed = responseCompleted;
                    context.Response.OnCompleted(() =>
                    {
                        completed?.SetResult();
                        return Task.CompletedTask;
                    });
                    PooledBufferTracker.TrackCurrentFlow();
                    await next(context);
                })
                .UseRouting()
                .UseEndpoints(endpoints => endpoints.MapGraphQL()));

        // the first request builds the executor and warms the caches of the error path
        using (var warmUpForm = CreateUploadForm(operations, map))
        {
            using var warmUpResponse =
                await server.SendMultipartRequestAsync(warmUpForm, "/graphql");
        }

        using var tracker = new PooledBufferTracker();
        responseCompleted = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        using var form = CreateUploadForm(operations, map);

        // act
        using var response = await server.SendMultipartRequestAsync(form, "/graphql");
        await responseCompleted.Task.WaitAsync(
            TimeSpan.FromSeconds(30),
            TestContext.Current.CancellationToken);

        // assert
        Assert.NotEqual(0, tracker.RentCount);
        Assert.Empty(tracker.UnreturnedBufferSizes);
    }

    private static void EnableBatchingOnUploadSchema(IServiceCollection services)
        => services
            .AddGraphQLServer("upload")
            .ModifyServerOptions(o => o.Batching = AllowedBatching.RequestBatching);

    private static MultipartFormDataContent CreateUploadForm(string operations, string map)
    {
        var form = new MultipartFormDataContent
        {
            { new StringContent(operations), "operations" },
            { new StringContent(map), "map" },
            { new StringContent("abc"), "1", "a.txt" },
            { new StringContent("def"), "2", "b.txt" }
        };
        form.Headers.Add(HttpHeaderKeys.Preflight, "1");
        return form;
    }

    private sealed class FailedAntiforgeryValidationFeature : IAntiforgeryValidationFeature
    {
        public bool IsValid => false;

        public Exception? Error => null;
    }

    private sealed class RecordingListener : ServerDiagnosticEventListener
    {
        public List<IError> Errors { get; } = [];

        public override void HttpRequestError(HttpContext context, IError error)
            => Errors.Add(error);
    }

    private sealed class PooledBufferTracker : EventListener
    {
        private static readonly AsyncLocal<bool> s_isTrackedFlow = new();
#if NET9_0_OR_GREATER
        private readonly Lock _sync = new();
#else
        private readonly object _sync = new();
#endif
        private readonly Dictionary<int, int> _rentedBuffers = [];
        private int _rentCount;

        public int RentCount
        {
            get
            {
                lock (_sync)
                {
                    return _rentCount;
                }
            }
        }

        public IReadOnlyCollection<int> UnreturnedBufferSizes
        {
            get
            {
                lock (_sync)
                {
                    return [.. _rentedBuffers.Values];
                }
            }
        }

        public static void TrackCurrentFlow() => s_isTrackedFlow.Value = true;

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            if (eventSource.Name == "System.Buffers.ArrayPoolEventSource")
            {
                EnableEvents(eventSource, EventLevel.Verbose);
            }
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            if (eventData.Payload is not { Count: >= 2 } payload)
            {
                return;
            }

            if (eventData.EventName == "BufferRented"
                && (!s_isTrackedFlow.Value || IsRentedByBufferedReadStream()))
            {
                return;
            }

            lock (_sync)
            {
                switch (eventData.EventName)
                {
                    case "BufferRented":
                        _rentCount++;
                        _rentedBuffers[(int)payload[0]!] = (int)payload[1]!;
                        break;

                    case "BufferReturned":
                        _rentedBuffers.Remove((int)payload[0]!);
                        break;
                }
            }
        }

        // MultipartReader never disposes its BufferedReadStream, so that buffer stays rented:
        // https://github.com/dotnet/aspnetcore/issues/57478
        private static bool IsRentedByBufferedReadStream()
            => new StackTrace().GetFrames().Any(
                frame => frame.GetMethod()?.DeclaringType == typeof(BufferedReadStream));
    }
}
