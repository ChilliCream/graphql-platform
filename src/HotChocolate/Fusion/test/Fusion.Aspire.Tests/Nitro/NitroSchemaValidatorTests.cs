using System.Net;
using System.Net.Http.Headers;
using System.Text;
using HotChocolate.Transport.Http;
using Microsoft.Extensions.Time.Testing;

namespace HotChocolate.Fusion.Aspire.Nitro;

public sealed class NitroSchemaValidatorTests
{
    private const string SchemaHash = "ABC123";
    private static readonly byte[] s_schema =
        Encoding.UTF8.GetBytes("type Query { ping: String }");

    [Fact]
    public async Task ValidateAsync_Should_ReturnPassed_When_ValidationSucceedsAfterProgress()
    {
        // arrange
        using var session = new ValidatorSession(
            Json(StartSuccess),
            Json(PollResult("""{"__typename":"OperationInProgress","state":"PROCESSING"}""")),
            Json(PollResult("""{"__typename":"ValidationInProgress","state":"PROCESSING"}""")),
            Json(PollResult("""{"__typename":"SchemaVersionValidationSuccess","state":"SUCCESS"}""")));

        // act
        var report = await session.ValidateAsync();

        // assert
        $"""
        Status: {report.Status}
        Request ID: {report.RequestId}
        Calls: {session.Handler.Requests.Count}
        Operations: {string.Join(", ", session.Handler.Requests.Select(request => request.OperationName))}
        Start wire: {DescribeStartRequest(session.Handler.Requests[0])}
        """.MatchInlineSnapshot(
            """
            Status: Passed
            Request ID: request-1
            Calls: 4
            Operations: ValidateNitroSchema, PollNitroSchemaValidation, PollNitroSchemaValidation, PollNitroSchemaValidation
            Start wire: multipart=True, apiId=True, stage=True, schema=True, apiKey=nitro-api-key
            """);
    }

    [Theory]
    [MemberData(nameof(KnownTerminalFailureData))]
    public async Task ValidateAsync_Should_ReturnStructuredViolations_When_TerminalFailureIsKnown(
        string error,
        string expected)
    {
        // arrange
        using var session = new ValidatorSession(
            Json(StartSuccess),
            Json(PollFailure(error)));

        // act
        var report = await session.ValidateAsync();

        // assert
        Assert.Equal(expected, DescribeViolations(report));
    }

    [Theory]
    [MemberData(nameof(StartErrorData))]
    public async Task ValidateAsync_Should_ReturnUnavailable_When_StartReturnsAnError(
        string typeName,
        string message)
    {
        // arrange
        using var session = new ValidatorSession(
            Json(StartFailure(typeName, message)));

        // act
        var report = await session.ValidateAsync();

        // assert
        Assert.Equal(
            $"Unavailable|request=-|reason={typeName}: {message}|calls=1",
            DescribeUnavailable(report, session.Handler.Requests.Count));
    }

    [Theory]
    [InlineData("UnauthorizedOperation", "The credential is invalid.")]
    [InlineData("SchemaVersionRequestNotFoundError", "The validation request does not exist.")]
    public async Task ValidateAsync_Should_ReturnUnavailable_When_PollReturnsAnError(
        string typeName,
        string message)
    {
        // arrange
        using var session = new ValidatorSession(
            Json(StartSuccess),
            Json(PollFailurePayload(typeName, message)));

        // act
        var report = await session.ValidateAsync();

        // assert
        Assert.Equal(
            $"Unavailable|request=request-1|reason={typeName}: {message}|calls=2",
            DescribeUnavailable(report, session.Handler.Requests.Count));
    }

    [Fact]
    public async Task ValidateAsync_Should_UseClientIdAndQueryMessage_When_ClientIsNullAndErrorsAreTruncated()
    {
        // arrange
        using var session = new ValidatorSession(
            Json(StartSuccess),
            Json(
                PollFailure(
                    """
                    {
                      "__typename": "PersistedQueryValidationError",
                      "message": "Client contracts failed.",
                      "clientId": "client-fallback",
                      "hasMoreErrors": true,
                      "client": null,
                      "queries": [
                        {
                          "hash": "operation-fallback",
                          "deployedTags": ["production"],
                          "message": "The operation no longer validates.",
                          "errors": []
                        }
                      ]
                    }
                    """)));

        // act
        var report = await session.ValidateAsync();

        // assert
        Assert.Equal(
            "Violations|more=True|clients=client-fallback/unknown>"
                + "operation-fallback[production]>"
                + "Client contract violations>PersistedQueryValidationError|-|-|-|-:-|-|"
                + "The operation no longer validates."
                + "|findings=-",
            DescribeViolations(report));
    }

    [Fact]
    public async Task ValidateAsync_Should_ReturnUnavailable_When_TransportFails()
    {
        // arrange
        using var session = new ValidatorSession(
            Throws(new HttpRequestException("connection refused")));

        // act
        var report = await session.ValidateAsync();

        // assert
        Assert.Equal(
            "Unavailable|request=-|reason=Nitro schema validation was unavailable: "
                + "connection refused|calls=1",
            DescribeUnavailable(report, session.Handler.Requests.Count));
    }

    [Fact]
    public async Task ValidateAsync_Should_ReturnUnavailable_When_RequestTimesOut()
    {
        // arrange
        using var session = new ValidatorSession(
            Throws(new TaskCanceledException("request timed out")));

        // act
        var report = await session.ValidateAsync();

        // assert
        Assert.Equal(
            "Unavailable|request=-|reason=Nitro schema validation timed out.|calls=1",
            DescribeUnavailable(report, session.Handler.Requests.Count));
    }

    [Fact]
    public async Task ValidateAsync_Should_ReturnUnavailableWithoutHttp_When_CredentialIsUnavailable()
    {
        // arrange
        using var session = new ValidatorSession();
        var credential = NitroCredential.Unavailable(
            NitroCredentialUnavailableReason.SessionMissing,
            "Sign in to Nitro first.");

        // act
        var report = await session.ValidateAsync(credential);

        // assert
        Assert.Equal(
            "Unavailable|request=-|reason=Sign in to Nitro first.|calls=0",
            DescribeUnavailable(report, session.Handler.Requests.Count));
    }

    [Theory]
    [MemberData(nameof(MalformedResponseData))]
    public async Task ValidateAsync_Should_ReturnUnavailable_When_ResponseIsMalformed(
        string response,
        string expectedReason)
    {
        // arrange
        using var session = new ValidatorSession(
            Json(StartSuccess),
            Json(response));

        // act
        var report = await session.ValidateAsync();

        // assert
        Assert.Equal(
            $"Unavailable|request=request-1|reason={expectedReason}|calls=2",
            DescribeUnavailable(report, session.Handler.Requests.Count));
    }

    [Fact]
    public async Task ValidateAsync_Should_ReturnUnavailable_When_ResponseContainsInvalidJson()
    {
        // arrange
        using var session = new ValidatorSession(
            Json(StartSuccess),
            Json("{"));

        // act
        var report = await session.ValidateAsync();

        // assert
        Assert.StartsWith(
            "Unavailable|request=request-1|reason=Nitro schema validation was unavailable:",
            DescribeUnavailable(report, session.Handler.Requests.Count),
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task ValidateAsync_Should_ReturnUnavailable_When_ResultTypeIsUnknown()
    {
        // arrange
        using var session = new ValidatorSession(
            Json(StartSuccess),
            Json(PollResult("""{"__typename":"FutureValidationResult","state":"SUCCESS"}""")));

        // act
        var report = await session.ValidateAsync();

        // assert
        Assert.Equal(
            "Unavailable|request=request-1|reason=Nitro returned the unknown validation result "
                + "'FutureValidationResult'.|calls=2",
            DescribeUnavailable(report, session.Handler.Requests.Count));
    }

    [Fact]
    public async Task ValidateAsync_Should_ReturnUnavailable_When_FailureTypeIsUnknown()
    {
        // arrange
        using var session = new ValidatorSession(
            Json(StartSuccess),
            Json(
                PollFailure(
                    """
                    {
                      "__typename": "FutureValidationError",
                      "message": "A future validation failed."
                    }
                    """)));

        // act
        var report = await session.ValidateAsync();

        // assert
        Assert.Equal(
            "Unavailable|request=request-1|reason=Nitro returned the unknown validation error "
                + "'FutureValidationError'.|calls=2",
            DescribeUnavailable(report, session.Handler.Requests.Count));
    }

    [Fact]
    public async Task ValidateAsync_Should_KeepKnownFindings_When_FailureAlsoContainsUnknownType()
    {
        // arrange
        using var session = new ValidatorSession(
            Json(StartSuccess),
            Json(
                PollFailure(
                    """
                    {
                      "__typename": "SchemaVersionSyntaxError",
                      "message": "Unexpected token.",
                      "line": 7,
                      "column": 9,
                      "position": 42
                    }
                    """,
                    """
                    {
                      "__typename": "FutureValidationError",
                      "message": "A future validation failed."
                    }
                    """)));

        // act
        var report = await session.ValidateAsync();

        // assert
        Assert.Equal(
            "Violations|more=False|clients=-|findings=Schema syntax errors>"
                + "SchemaVersionSyntaxError|-|-|-|7:9|-|Unexpected token.",
            DescribeViolations(report));
    }

    [Fact]
    public async Task ValidateAsync_Should_EnforceResponseLimit_When_ContentLengthIsAbsent()
    {
        // arrange
        var oversizedContent = ChunkedContent(new byte[2 * 1024 * 1024 + 1]);
        var contentLength = oversizedContent.Headers.ContentLength;
        using var session = new ValidatorSession(
            Json(StartSuccess),
            Returns(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = oversizedContent
                }));

        // act
        var report = await session.ValidateAsync();

        // assert
        $"""
        Status: {report.Status}
        Reason: {report.UnavailableReason}
        Content-Length: {contentLength?.ToString() ?? "<none>"}
        Calls: {session.Handler.Requests.Count}
        """.MatchInlineSnapshot(
            """
            Status: Unavailable
            Reason: Nitro schema validation was unavailable: Nitro returned a schema validation response that exceeded the size limit.
            Content-Length: <none>
            Calls: 2
            """);
    }

    [Fact]
    public async Task ValidateAsync_Should_NotRetry_When_StartReturnsTransientStatus()
    {
        // arrange
        using var session = new ValidatorSession(
            Status(HttpStatusCode.ServiceUnavailable),
            Json(StartSuccess));

        // act
        var report = await session.ValidateAsync();

        // assert
        Assert.Equal(
            "Unavailable|request=-|reason=Nitro returned HTTP 503 (ServiceUnavailable).|calls=1",
            DescribeUnavailable(report, session.Handler.Requests.Count));
    }

    [Fact]
    public async Task ValidateAsync_Should_RetryTransientFailures_When_Polling()
    {
        // arrange
        using var session = new ValidatorSession(
            Json(StartSuccess),
            Status(HttpStatusCode.ServiceUnavailable),
            Status(HttpStatusCode.TooManyRequests),
            Json(PollResult("""{"__typename":"SchemaVersionValidationSuccess","state":"SUCCESS"}""")));

        // act
        var report = await session.ValidateAsync();

        // assert
        Assert.Equal(
            "Passed|request=request-1|calls=4|operations=ValidateNitroSchema,"
                + "PollNitroSchemaValidation,PollNitroSchemaValidation,"
                + "PollNitroSchemaValidation",
            $"{report.Status}|request={report.RequestId}|calls={session.Handler.Requests.Count}"
                + $"|operations={string.Join(',', session.Handler.Requests.Select(r => r.OperationName))}");
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    public async Task ValidateAsync_Should_NotRetry_When_PollReturnsNonTransientStatus(
        HttpStatusCode statusCode)
    {
        // arrange
        using var session = new ValidatorSession(
            Json(StartSuccess),
            Status(statusCode),
            Json(PollResult("""{"__typename":"SchemaVersionValidationSuccess","state":"SUCCESS"}""")));

        // act
        var report = await session.ValidateAsync();

        // assert
        Assert.Equal(
            $"Unavailable|request=request-1|reason=Nitro returned HTTP {(int)statusCode} "
                + $"({statusCode}).|calls=2",
            DescribeUnavailable(report, session.Handler.Requests.Count));
    }

    public static TheoryData<string, string> KnownTerminalFailureData()
        => new()
        {
            {
                """
                {
                  "__typename": "PersistedQueryValidationError",
                  "message": "Client contracts failed.",
                  "clientId": "client-1",
                  "hasMoreErrors": false,
                  "client": {
                    "id": "client-1",
                    "name": "Web"
                  },
                  "queries": [
                    {
                      "hash": "operation-1",
                      "deployedTags": ["production"],
                      "message": "The operation is invalid.",
                      "errors": [
                        {
                          "message": "Unknown field.",
                          "code": "HC001",
                          "path": "query.product",
                          "locations": [{"line": 2, "column": 3}]
                        }
                      ]
                    }
                  ]
                }
                """,
                "Violations|more=False|clients=client-1/Web>operation-1[production]>"
                    + "Client contract violations>PersistedQueryValidationError|HC001|-|"
                    + "query.product|2:3|-|Unknown field."
                    + "|findings=-"
            },
            {
                """
                {
                  "__typename": "SchemaVersionChangeViolationError",
                  "message": "Breaking schema changes.",
                  "changes": [
                    {
                      "__typename": "ObjectModifiedChange",
                      "severity": "BREAKING",
                      "coordinate": "Product",
                      "changes": [
                        {
                          "__typename": "FieldRemovedChange",
                          "severity": "BREAKING",
                          "coordinate": "Product.name",
                          "typeName": "Product",
                          "fieldName": "name"
                        }
                      ]
                    },
                    {
                      "__typename": "ObjectModifiedChange",
                      "severity": "BREAKING",
                      "coordinate": "Query",
                      "changes": [
                        {
                          "__typename": "OutputFieldChanged",
                          "severity": "BREAKING",
                          "coordinate": "Query.product",
                          "fieldName": "product",
                          "changes": [
                            {
                              "__typename": "TypeChanged",
                              "severity": "BREAKING",
                              "oldType": "Product",
                              "newType": "Product!"
                            }
                          ]
                        }
                      ]
                    }
                  ]
                }
                """,
                "Violations|more=False|clients=-|findings=Schema change violations>"
                    + "FieldRemovedChange|-|Product.name|-|-:-|BREAKING|"
                    + "Field 'name' was removed from type 'Product'.,"
                    + "Schema change violations>TypeChanged|-|Query.product|-|-:-|BREAKING|"
                    + "Type changed from 'Product' to 'Product!'."
            },
            {
                """
                {
                  "__typename": "InvalidGraphQLSchemaError",
                  "message": "The schema is invalid.",
                  "errors": [
                    {
                      "message": "Type Query is missing.",
                      "code": "HC002"
                    }
                  ]
                }
                """,
                "Violations|more=False|clients=-|findings=Invalid GraphQL schema>"
                    + "InvalidGraphQLSchemaError|HC002|-|-|-:-|-|Type Query is missing."
            },
            {
                """
                {
                  "__typename": "OpenApiCollectionValidationError",
                  "message": "OpenAPI failed.",
                  "collections": [
                    {
                      "openApiCollection": {
                        "id": "openapi-1",
                        "name": "Store API"
                      },
                      "entities": [
                        {
                          "__typename": "OpenApiCollectionValidationEndpoint",
                          "httpMethod": "GET",
                          "route": "/products",
                          "errors": [
                            {
                              "__typename": "OpenApiCollectionValidationDocumentError",
                              "message": "The endpoint is missing.",
                              "code": "OA001",
                              "path": "paths./products",
                              "locations": [{"line": 10, "column": 5}]
                            }
                          ]
                        }
                      ]
                    }
                  ]
                }
                """,
                "Violations|more=False|clients=-|findings=OpenAPI>"
                    + "OpenApiCollectionValidationDocumentError|OA001|-|paths./products|10:5|"
                    + "openapi-1|GET /products|Store API, GET /products: The endpoint is missing."
            },
            {
                """
                {
                  "__typename": "McpFeatureCollectionValidationError",
                  "message": "MCP failed.",
                  "collections": [
                    {
                      "mcpFeatureCollection": {
                        "id": "mcp-1",
                        "name": "Store tools"
                      },
                      "entities": [
                        {
                          "__typename": "McpFeatureCollectionValidationTool",
                          "name": "findProduct",
                          "errors": [
                            {
                              "__typename": "McpFeatureCollectionValidationDocumentError",
                              "message": "The tool is invalid.",
                              "code": "MCP001",
                              "path": "tools.findProduct",
                              "locations": [{"line": 4, "column": 2}]
                            }
                          ]
                        }
                      ]
                    }
                  ]
                }
                """,
                "Violations|more=False|clients=-|findings=MCP>"
                    + "McpFeatureCollectionValidationDocumentError|MCP001|-|tools.findProduct|4:2|"
                    + "mcp-1|findProduct|Store tools, findProduct: The tool is invalid."
            },
            {
                """
                {
                  "__typename": "SchemaVersionSyntaxError",
                  "message": "Unexpected token.",
                  "line": 7,
                  "column": 9,
                  "position": 42
                }
                """,
                "Violations|more=False|clients=-|findings=Schema syntax errors>"
                    + "SchemaVersionSyntaxError|-|-|-|7:9|-|Unexpected token."
            },
            {
                """
                {
                  "__typename": "OperationsAreNotAllowedError",
                  "message": "Operations are forbidden.",
                  "operationName": "ForbiddenOperation"
                }
                """,
                "Violations|more=False|clients=-|findings=Operation policy errors>"
                    + "OperationsAreNotAllowedError|-|-|-|-:-|ForbiddenOperation|"
                    + "Operations are forbidden."
            },
            {
                """
                {
                  "__typename": "ProcessingTimeoutError",
                  "message": "Processing timed out."
                }
                """,
                "Violations|more=False|clients=-|findings=Processing errors>"
                    + "ProcessingTimeoutError|-|-|-|-:-|-|Processing timed out."
            },
            {
                """
                {
                  "__typename": "ReadyTimeoutError",
                  "message": "The task did not become ready."
                }
                """,
                "Violations|more=False|clients=-|findings=Processing errors>"
                    + "ReadyTimeoutError|-|-|-|-:-|-|The task did not become ready."
            },
            {
                """
                {
                  "__typename": "SchemaVersionChangeViolationError",
                  "message": "Non-breaking schema changes.",
                  "changes": [
                    {
                      "__typename": "ObjectModifiedChange",
                      "severity": "NON_BREAKING",
                      "coordinate": "Product",
                      "changes": [
                        {
                          "__typename": "FieldAddedChange",
                          "severity": "NON_BREAKING",
                          "coordinate": "Product.price",
                          "typeName": "Product",
                          "fieldName": "price"
                        }
                      ]
                    }
                  ]
                }
                """,
                "Violations|more=False|clients=-|findings=Schema change violations>"
                    + "FieldAddedChange|-|Product.price|-|-:-|NON_BREAKING|"
                    + "Field 'price' was added to type 'Product'."
            },
            {
                """
                {
                  "__typename": "UnexpectedProcessingError",
                  "message": "Processing failed unexpectedly."
                }
                """,
                "Violations|more=False|clients=-|findings=Processing errors>"
                    + "UnexpectedProcessingError|-|-|-|-:-|-|Processing failed unexpectedly."
            }
        };

    public static TheoryData<string, string> StartErrorData()
        => new()
        {
            { "UnauthorizedOperation", "The credential is invalid." },
            { "InvalidSourceMetadataInputError", "The source metadata is invalid." },
            { "ApiNotFoundError", "The API does not exist." },
            { "StageNotFoundError", "The stage does not exist." },
            { "SchemaNotFoundError", "The schema does not exist." }
        };

    public static TheoryData<string, string> MalformedResponseData()
        => new()
        {
            {
                """{"data":{}}""",
                "Nitro returned a malformed validation polling response."
            },
            {
                """{"errors":[{"message":"GraphQL failed."}]}""",
                "Nitro returned GraphQL errors."
            },
            {
                """
                {
                  "data": {
                    "pollSchemaVersionValidationRequest": {
                      "errors": [],
                      "result": null
                    }
                  }
                }
                """,
                "Nitro returned no schema validation result."
            },
            {
                PollFailure(""),
                "Nitro returned a validation failure without findings."
            }
        };

    private static string DescribeStartRequest(RecordedHttpRequest request)
    {
        var isMultipart = request.ContentType == "multipart/form-data";

        return $"multipart={isMultipart}, "
            + $"apiId={request.Body.Contains("api-1", StringComparison.Ordinal)}, "
            + $"stage={request.Body.Contains("production", StringComparison.Ordinal)}, "
            + $"schema={request.Body.Contains("type Query { ping: String }", StringComparison.Ordinal)}, "
            + $"apiKey={request.ApiKey}";
    }

    private static string DescribeViolations(NitroSchemaValidationReport report)
    {
        var clients = report.Clients.Count == 0
            ? "-"
            : string.Join(
                ';',
                report.Clients.Select(
                    client =>
                        $"{client.ClientId}/{client.ClientName}>"
                        + string.Join(
                            ',',
                            client.Operations.Select(
                                operation =>
                                    $"{operation.Hash}[{string.Join(',', operation.DeployedTags)}]>"
                                    + string.Join(
                                        ',',
                                        operation.Errors.Select(DescribeFinding))))));
        var findings = report.Findings.Count == 0
            ? "-"
            : string.Join(',', report.Findings.Select(DescribeFinding));

        return $"{report.Status}|more={report.HasMoreClientErrors}|clients={clients}"
            + $"|findings={findings}";
    }

    private static string DescribeFinding(NitroSchemaValidationFinding finding)
        => $"{finding.Group}>{finding.Kind}|{finding.Code ?? "-"}|{finding.Coordinate ?? "-"}|"
            + $"{finding.Path ?? "-"}|{finding.Line?.ToString() ?? "-"}:"
            + $"{finding.Column?.ToString() ?? "-"}|{finding.Identity ?? "-"}|{finding.Message}";

    private static string DescribeUnavailable(
        NitroSchemaValidationReport report,
        int callCount)
        => $"{report.Status}|request={report.RequestId ?? "-"}|reason={report.UnavailableReason}"
            + $"|calls={callCount}";

    private static string StartFailure(string typeName, string message)
        => $$"""
        {
          "data": {
            "validateSchema": {
              "id": null,
              "errors": [
                {
                  "__typename": "{{typeName}}",
                  "message": "{{message}}"
                }
              ]
            }
          }
        }
        """;

    private static string PollFailurePayload(string typeName, string message)
        => $$"""
        {
          "data": {
            "pollSchemaVersionValidationRequest": {
              "errors": [
                {
                  "__typename": "{{typeName}}",
                  "message": "{{message}}"
                }
              ],
              "result": null
            }
          }
        }
        """;

    private static string PollFailure(params string[] errors)
        => $$"""
        {
          "data": {
            "pollSchemaVersionValidationRequest": {
              "errors": [],
              "result": {
                "__typename": "SchemaVersionValidationFailed",
                "state": "FAILED",
                "errors": [{{string.Join(',', errors)}}]
              }
            }
          }
        }
        """;

    private static string PollResult(string result)
        => $$"""
        {
          "data": {
            "pollSchemaVersionValidationRequest": {
              "errors": [],
              "result": {{result}}
            }
          }
        }
        """;

    private static ResponseStep Json(string json)
        => Returns(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });

    private static ResponseStep Status(HttpStatusCode statusCode)
        => Returns(
            new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(string.Empty, Encoding.UTF8, "text/plain")
            });

    private static ResponseStep Returns(HttpResponseMessage response)
        => _ => Task.FromResult(response);

    private static ResponseStep Throws(Exception exception)
        => _ => Task.FromException<HttpResponseMessage>(exception);

    private static StreamContent ChunkedContent(byte[] content)
    {
        var result = new StreamContent(new NonSeekableReadStream(content));
        result.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        return result;
    }

    private const string StartSuccess =
        """{"data":{"validateSchema":{"id":"request-1","errors":[]}}}""";

    private delegate Task<HttpResponseMessage> ResponseStep(CancellationToken cancellationToken);

    private sealed class ValidatorSession : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly FakeTimeProvider _timeProvider = new();
        private readonly NitroSchemaValidator _validator;

        public ValidatorSession(params ResponseStep[] responses)
        {
            Handler = new ScriptedHttpMessageHandler(responses);
            _httpClient = new HttpClient(Handler);
            _validator = new NitroSchemaValidator(
                GraphQLHttpClient.Create(_httpClient, disposeHttpClient: false),
                _timeProvider,
                new RecordingLogger<NitroSchemaValidator>());
        }

        public ScriptedHttpMessageHandler Handler { get; }

        public async Task<NitroSchemaValidationReport> ValidateAsync(
            NitroCredential? credential = null)
        {
            var validation = _validator.ValidateAsync(
                new NitroConnection(
                    new Uri("https://api.example.test"),
                    new Uri("https://api.example.test/graphql"),
                    credential ?? NitroCredential.FromApiKey("nitro-api-key")),
                "api-1",
                "production",
                s_schema,
                SchemaHash,
                TestContext.Current.CancellationToken);

            for (var attempt = 0; attempt < 100 && !validation.IsCompleted; attempt++)
            {
                await Task.Yield();
                _timeProvider.Advance(TimeSpan.FromSeconds(5));
            }

            return await validation.WaitAsync(
                TimeSpan.FromSeconds(10),
                TestContext.Current.CancellationToken);
        }

        public void Dispose() => _httpClient.Dispose();
    }

    private sealed class ScriptedHttpMessageHandler(IEnumerable<ResponseStep> responses)
        : HttpMessageHandler
    {
        private readonly Queue<ResponseStep> _responses = new(responses);

        public List<RecordedHttpRequest> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var body = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken);
            var operationName = body.Contains(
                NitroOperationDocuments.ValidateSchemaOperationName,
                StringComparison.Ordinal)
                    ? NitroOperationDocuments.ValidateSchemaOperationName
                    : body.Contains(
                        NitroOperationDocuments.PollSchemaValidationOperationName,
                        StringComparison.Ordinal)
                        ? NitroOperationDocuments.PollSchemaValidationOperationName
                        : "unknown";
            var apiKey = request.Headers.TryGetValues(NitroRequestHeaders.ApiKey, out var values)
                ? values.Single()
                : null;

            Requests.Add(
                new RecordedHttpRequest(
                    request.Content?.Headers.ContentType?.MediaType,
                    body,
                    operationName,
                    apiKey));

            if (!_responses.TryDequeue(out var response))
            {
                throw new InvalidOperationException("No scripted Nitro response remains.");
            }

            return await response(cancellationToken);
        }
    }

    private sealed record RecordedHttpRequest(
        string? ContentType,
        string Body,
        string OperationName,
        string? ApiKey);

    private sealed class NonSeekableReadStream(byte[] content) : Stream
    {
        private readonly MemoryStream _stream = new(content, writable: false);

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
            => _stream.Read(buffer, offset, count);

        public override int Read(Span<byte> buffer) => _stream.Read(buffer);

        public override ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default)
            => _stream.ReadAsync(buffer, cancellationToken);

        public override long Seek(long offset, SeekOrigin origin)
            => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count)
            => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _stream.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
