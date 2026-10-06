using ChilliCream.Nitro.Client;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Telemetry.Client;

public sealed class TelemetryClientTests
{
    private const string WorkspaceId = "workspace-1";

    private const string PageInfo = """
        {
          "__typename": "PageInfo",
          "hasPreviousPage": false,
          "hasNextPage": true,
          "endCursor": "cursor-2",
          "startCursor": "cursor-1"
        }
        """;

    private static readonly DateTimeOffset s_from = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset s_to = new(2026, 1, 1, 1, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ListTracesAsync_Should_MapRowsAndPageInfo_When_ResponseContainsSpans()
    {
        // arrange
        // the second span has no service.name resource attribute
        var api = new FakeNitroApi(
            $$"""
            {
              "data": {
                "workspaceById": {
                  "__typename": "Workspace",
                  "spans": {
                    "__typename": "WorkspaceSpansConnection",
                    "edges": [
                      {
                        "__typename": "WorkspaceSpansEdge",
                        "cursor": "cursor-1",
                        "node": {
                          "__typename": "OpenTelemetryHttpServerSpan",
                          "traceId": "trace-1",
                          "spanId": "span-1",
                          "seeker": "seeker-1",
                          "spanName": "GET /products",
                          "spanKind": "SERVER",
                          "duration": 12.5,
                          "epoch": 1767225600000.5,
                          "statusCode": "OK",
                          "resourceAttributes": [
                            { "__typename": "Attribute", "key": "host.name", "value": "node-1" },
                            { "__typename": "Attribute", "key": "service.name", "value": "products" }
                          ]
                        }
                      },
                      {
                        "__typename": "WorkspaceSpansEdge",
                        "cursor": "cursor-2",
                        "node": {
                          "__typename": "OpenTelemetryDbSpan",
                          "traceId": "trace-2",
                          "spanId": "span-2",
                          "seeker": "seeker-2",
                          "spanName": "SELECT products",
                          "spanKind": "CLIENT",
                          "duration": 3,
                          "epoch": 1767225601000,
                          "statusCode": "ERROR",
                          "resourceAttributes": [
                            { "__typename": "Attribute", "key": "host.name", "value": "node-2" }
                          ]
                        }
                      }
                    ],
                    "pageInfo": {{PageInfo}}
                  }
                }
              }
            }
            """);
        var client = api.CreateTelemetryClient();

        // act
        var page = await client.ListTracesAsync(
            WorkspaceId,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            TestContext.Current.CancellationToken);

        // assert
        page.MatchInlineSnapshot(
            """
            {
              "Items": [
                {
                  "TraceId": "trace-1",
                  "SpanId": "span-1",
                  "Seeker": "seeker-1",
                  "SpanName": "GET /products",
                  "SpanKind": "SERVER",
                  "DurationMs": 12.5,
                  "Start": 1767225600000.5,
                  "StatusCode": "OK",
                  "ServiceName": "products"
                },
                {
                  "TraceId": "trace-2",
                  "SpanId": "span-2",
                  "Seeker": "seeker-2",
                  "SpanName": "SELECT products",
                  "SpanKind": "CLIENT",
                  "DurationMs": 3.0,
                  "Start": 1767225601000.0,
                  "StatusCode": "ERROR",
                  "ServiceName": ""
                }
              ],
              "EndCursor": "cursor-2",
              "HasNextPage": true
            }
            """);
    }

    [Fact]
    public async Task ListTracesAsync_Should_SendVariablesAndDefaultSpanKinds_When_NoSpanKindsAreRequested()
    {
        // arrange
        var api = new FakeNitroApi("""{ "data": { "workspaceById": null } }""");
        var client = api.CreateTelemetryClient();
        var filter = new OpenTelemetryFilterInput
        {
            Attribute = new OpenTelemetryAttributePredicateInput
            {
                Key = "http.method",
                Kind = OpenTelemetryAttributeKind.Span,
                Condition = new OpenTelemetryAttributeConditionInput
                {
                    Eq = new OpenTelemetryAttributeValueInput { String = "GET" }
                }
            }
        };

        // act
        await client.ListTracesAsync(
            WorkspaceId,
            filter,
            ["dev", "prod"],
            null,
            s_from,
            s_to,
            25,
            "cursor-0",
            TestContext.Current.CancellationToken);

        // assert
        api.Variables.MatchInlineSnapshot(
            """
            {
              "workspaceId": "workspace-1",
              "filter": {
                "attribute": {
                  "condition": {
                    "eq": {
                      "string": "GET"
                    }
                  },
                  "key": "http.method",
                  "kind": "SPAN"
                }
              },
              "environments": [
                "dev",
                "prod"
              ],
              "spanKinds": [
                "SERVER",
                "CONSUMER"
              ],
              "from": "2026-01-01T00:00:00Z",
              "to": "2026-01-01T01:00:00Z",
              "first": 25,
              "after": "cursor-0"
            }
            """);
    }

    [Fact]
    public async Task ListTracesAsync_Should_SendRequestedSpanKinds_When_SpanKindsAreProvided()
    {
        // arrange
        var api = new FakeNitroApi("""{ "data": { "workspaceById": null } }""");
        var client = api.CreateTelemetryClient();

        // act
        await client.ListTracesAsync(
            WorkspaceId,
            null,
            null,
            [OpenTelemetrySpanKind.Client],
            null,
            null,
            null,
            null,
            TestContext.Current.CancellationToken);

        // assert
        api.Variables.MatchInlineSnapshot(
            """
            {
              "workspaceId": "workspace-1",
              "filter": null,
              "environments": null,
              "spanKinds": [
                "CLIENT"
              ],
              "from": null,
              "to": null,
              "first": null,
              "after": null
            }
            """);
    }

    [Fact]
    public async Task ListTracesAsync_Should_ReturnEmptyPage_When_WorkspaceIsNotFound()
    {
        // arrange
        var api = new FakeNitroApi("""{ "data": { "workspaceById": null } }""");
        var client = api.CreateTelemetryClient();

        // act
        var page = await client.ListTracesAsync(
            WorkspaceId,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            TestContext.Current.CancellationToken);

        // assert
        page.MatchInlineSnapshot(
            """
            {
              "Items": [],
              "EndCursor": null,
              "HasNextPage": false
            }
            """);
    }

    [Fact]
    public async Task GetTraceAsync_Should_MapSpansEventsAndAttributes_When_TraceIsFound()
    {
        // arrange
        var api = new FakeNitroApi(
            """
            {
              "data": {
                "workspaceById": {
                  "__typename": "Workspace",
                  "traceById": {
                    "__typename": "OpenTelemetryTrace",
                    "spanCount": 3,
                    "spansTruncated": true,
                    "totalDuration": 42.5,
                    "spans": [
                      {
                        "__typename": "OpenTelemetryDefaultSpan",
                        "spanId": "span-1",
                        "parentSpanId": "span-0",
                        "spanName": "work",
                        "spanKind": "INTERNAL",
                        "duration": 7.25,
                        "epoch": 1767225600000.5,
                        "statusCode": "ERROR",
                        "statusMessage": "boom",
                        "resourceAttributes": [
                          { "__typename": "Attribute", "key": "service.name", "value": "products" }
                        ],
                        "spanAttributes": [
                          { "__typename": "Attribute", "key": "retry", "value": "2" },
                          { "__typename": "Attribute", "key": "queue", "value": "default" }
                        ],
                        "events": [
                          {
                            "__typename": "OpenTelemetryTraceEvent",
                            "name": "exception",
                            "epoch": 1767225600005,
                            "attributes": [
                              { "__typename": "Attribute", "key": "exception.type", "value": "TimeoutException" }
                            ]
                          },
                          {
                            "__typename": "OpenTelemetryTraceEvent",
                            "name": "retry",
                            "epoch": 1767225600006,
                            "attributes": []
                          }
                        ]
                      }
                    ]
                  }
                }
              }
            }
            """);
        var client = api.CreateTelemetryClient();

        // act
        var trace = await client.GetTraceAsync(
            WorkspaceId,
            "trace-1",
            null,
            null,
            TestContext.Current.CancellationToken);

        // assert
        trace.MatchInlineSnapshot(
            """
            {
              "SpanCount": 3,
              "SpansTruncated": true,
              "TotalDuration": 42.5,
              "Spans": [
                {
                  "SpanId": "span-1",
                  "ParentSpanId": "span-0",
                  "SpanName": "work",
                  "SpanKind": "INTERNAL",
                  "DurationMs": 7.25,
                  "Start": 1767225600000.5,
                  "StatusCode": "ERROR",
                  "StatusMessage": "boom",
                  "ResourceAttributes": [
                    {
                      "Key": "service.name",
                      "Value": "products"
                    }
                  ],
                  "SpanAttributes": [
                    {
                      "Key": "retry",
                      "Value": "2"
                    },
                    {
                      "Key": "queue",
                      "Value": "default"
                    }
                  ],
                  "Events": [
                    {
                      "Name": "exception",
                      "Start": 1767225600005.0,
                      "Attributes": [
                        {
                          "Key": "exception.type",
                          "Value": "TimeoutException"
                        }
                      ]
                    },
                    {
                      "Name": "retry",
                      "Start": 1767225600006.0,
                      "Attributes": []
                    }
                  ],
                  "Data": null
                }
              ]
            }
            """);
    }

    [Fact]
    public async Task GetTraceAsync_Should_MapTypedData_When_SpansAreOfEveryKind()
    {
        // arrange
        string[] spans =
        [
            Span(
                "OpenTelemetryHttpClientSpan",
                "http-client",
                """
                "http": {
                  "__typename": "OpenTelemetryHttpClientSpanAttribute",
                  "flavor": "2.0",
                  "method": "POST",
                  "scheme": "https",
                  "statusCode": 201,
                  "url": "https://inventory.example/items",
                  "userAgent": "client/1.0"
                }
                """),
            Span(
                "OpenTelemetryHttpServerSpan",
                "http-server",
                """
                "http": {
                  "__typename": "OpenTelemetryHttpServerSpanAttributes",
                  "flavor": "1.1",
                  "method": "GET",
                  "scheme": "http",
                  "statusCode": 404,
                  "url": "/products/1",
                  "userAgent": null
                }
                """),
            Span(
                "OpenTelemetryDbSpan",
                "db",
                """
                "db": {
                  "__typename": "OpenTelemetryDbSpanAttributes",
                  "connectionString": "Host=db",
                  "instance": "primary",
                  "name": "products",
                  "operation": "SELECT",
                  "statement": "SELECT * FROM products",
                  "system": "postgresql",
                  "url": "postgres://db/products",
                  "user": "app"
                }
                """),
            Span(
                "OpenTelemetryGraphQLOperationSpan",
                "operation",
                """
                "document": {
                  "__typename": "OpenTelemetryGraphQLOperationSpanDocumentAttributes",
                  "body": "query GetProduct { product { id } }",
                  "id": "doc-1"
                },
                "operation": {
                  "__typename": "OpenTelemetryGraphQLOperationSpanOperationAttributes",
                  "hash": "hash-1",
                  "kind": "query",
                  "name": "GetProduct"
                }
                """),
            Span(
                "OpenTelemetryGraphQLResolverSpan",
                "resolver",
                """
                "selection": {
                  "__typename": "OpenTelemetryGraphQLResolverSpanSelectionAttributes",
                  "field": {
                    "__typename": "OpenTelemetryGraphQLResolverSpanFieldAttributes",
                    "coordinate": "Query.product",
                    "declaringType": "Query",
                    "name": "product"
                  },
                  "name": "product",
                  "path": "/product",
                  "type": "Product"
                }
                """),
            Span("OpenTelemetryDefaultSpan", "default")
        ];
        var api = new FakeNitroApi(
            $$"""
            {
              "data": {
                "workspaceById": {
                  "__typename": "Workspace",
                  "traceById": {
                    "__typename": "OpenTelemetryTrace",
                    "spanCount": 6,
                    "spansTruncated": false,
                    "totalDuration": 10,
                    "spans": [{{string.Join(",", spans)}}]
                  }
                }
              }
            }
            """);
        var client = api.CreateTelemetryClient();

        // act
        var trace = await client.GetTraceAsync(
            WorkspaceId,
            "trace-1",
            null,
            null,
            TestContext.Current.CancellationToken);

        // assert
        trace!
            .Spans.Select(span => new
            {
                span.SpanId,
                DataType = span.Data?.GetType().Name,
                span.Data
            })
            .MatchInlineSnapshot(
                """
                [
                  {
                    "SpanId": "http-client",
                    "DataType": "HttpTraceSpanData",
                    "Data": {
                      "Flavor": "2.0",
                      "Method": "POST",
                      "Scheme": "https",
                      "StatusCode": 201,
                      "Url": "https://inventory.example/items",
                      "UserAgent": "client/1.0"
                    }
                  },
                  {
                    "SpanId": "http-server",
                    "DataType": "HttpTraceSpanData",
                    "Data": {
                      "Flavor": "1.1",
                      "Method": "GET",
                      "Scheme": "http",
                      "StatusCode": 404,
                      "Url": "/products/1",
                      "UserAgent": null
                    }
                  },
                  {
                    "SpanId": "db",
                    "DataType": "DatabaseTraceSpanData",
                    "Data": {
                      "ConnectionString": "Host=db",
                      "Instance": "primary",
                      "Name": "products",
                      "Operation": "SELECT",
                      "Statement": "SELECT * FROM products",
                      "System": "postgresql",
                      "Url": "postgres://db/products",
                      "User": "app"
                    }
                  },
                  {
                    "SpanId": "operation",
                    "DataType": "GraphQLOperationTraceSpanData",
                    "Data": {
                      "Document": {
                        "Body": "query GetProduct { product { id } }",
                        "Id": "doc-1"
                      },
                      "Operation": {
                        "Hash": "hash-1",
                        "Kind": "query",
                        "Name": "GetProduct"
                      }
                    }
                  },
                  {
                    "SpanId": "resolver",
                    "DataType": "GraphQLResolverTraceSpanData",
                    "Data": {
                      "Selection": {
                        "Field": {
                          "Coordinate": "Query.product",
                          "DeclaringType": "Query",
                          "Name": "product"
                        },
                        "Name": "product",
                        "Path": "/product",
                        "Type": "Product"
                      }
                    }
                  },
                  {
                    "SpanId": "default",
                    "DataType": null,
                    "Data": null
                  }
                ]
                """);
    }

    [Fact]
    public async Task GetTraceAsync_Should_ReturnNullSpanCount_When_ServerDoesNotReportIt()
    {
        // arrange
        var api = new FakeNitroApi(
            """
            {
              "data": {
                "workspaceById": {
                  "__typename": "Workspace",
                  "traceById": {
                    "__typename": "OpenTelemetryTrace",
                    "spanCount": null,
                    "spansTruncated": true,
                    "totalDuration": 0,
                    "spans": []
                  }
                }
              }
            }
            """);
        var client = api.CreateTelemetryClient();

        // act
        var trace = await client.GetTraceAsync(
            WorkspaceId,
            "trace-1",
            null,
            null,
            TestContext.Current.CancellationToken);

        // assert
        trace.MatchInlineSnapshot(
            """
            {
              "SpanCount": null,
              "SpansTruncated": true,
              "TotalDuration": 0.0,
              "Spans": []
            }
            """);
    }

    [Fact]
    public async Task GetTraceAsync_Should_ReturnNull_When_TraceIsNotFound()
    {
        // arrange
        var api = new FakeNitroApi(
            """
            {
              "data": {
                "workspaceById": { "__typename": "Workspace", "traceById": null }
              }
            }
            """);
        var client = api.CreateTelemetryClient();

        // act
        var trace = await client.GetTraceAsync(
            WorkspaceId,
            "trace-1",
            null,
            null,
            TestContext.Current.CancellationToken);

        // assert
        Assert.Null(trace);
    }

    [Fact]
    public async Task ListLogsAsync_Should_MapRowsAndPageInfo_When_ResponseContainsLogs()
    {
        // arrange
        // the second log has no service.name resource attribute
        var api = new FakeNitroApi(
            $$"""
            {
              "data": {
                "workspaceById": {
                  "__typename": "Workspace",
                  "logs": {
                    "__typename": "WorkspaceLogsConnection",
                    "edges": [
                      {
                        "__typename": "WorkspaceLogsEdge",
                        "cursor": "cursor-1",
                        "node": {
                          "__typename": "OpenTelemetryLog",
                          "id": "log-1",
                          "epoch": 1767225600000.5,
                          "severityText": "Error",
                          "severityNumber": 17,
                          "body": "request failed",
                          "traceId": "trace-1",
                          "spanId": "span-1",
                          "resourceAttributes": [
                            { "__typename": "Attribute", "key": "host.name", "value": "node-1" },
                            { "__typename": "Attribute", "key": "service.name", "value": "products" }
                          ]
                        }
                      },
                      {
                        "__typename": "WorkspaceLogsEdge",
                        "cursor": "cursor-2",
                        "node": {
                          "__typename": "OpenTelemetryLog",
                          "id": "log-2",
                          "epoch": 1767225601000,
                          "severityText": "Info",
                          "severityNumber": 9,
                          "body": "started",
                          "traceId": "",
                          "spanId": "",
                          "resourceAttributes": []
                        }
                      }
                    ],
                    "pageInfo": {{PageInfo}}
                  }
                }
              }
            }
            """);
        var client = api.CreateTelemetryClient();

        // act
        var page = await client.ListLogsAsync(
            WorkspaceId,
            null,
            null,
            null,
            null,
            null,
            null,
            TestContext.Current.CancellationToken);

        // assert
        page.MatchInlineSnapshot(
            """
            {
              "Items": [
                {
                  "Id": "log-1",
                  "Start": 1767225600000.5,
                  "SeverityText": "Error",
                  "SeverityNumber": 17,
                  "Body": "request failed",
                  "TraceId": "trace-1",
                  "SpanId": "span-1",
                  "ServiceName": "products"
                },
                {
                  "Id": "log-2",
                  "Start": 1767225601000.0,
                  "SeverityText": "Info",
                  "SeverityNumber": 9,
                  "Body": "started",
                  "TraceId": "",
                  "SpanId": "",
                  "ServiceName": ""
                }
              ],
              "EndCursor": "cursor-2",
              "HasNextPage": true
            }
            """);
    }

    [Fact]
    public async Task GetLogAsync_Should_MapDetailAndTypedAttributes_When_LogIsFound()
    {
        // arrange
        var api = new FakeNitroApi(
            """
            {
              "data": {
                "workspaceById": {
                  "__typename": "Workspace",
                  "logById": {
                    "__typename": "OpenTelemetryLog",
                    "id": "log-1",
                    "epoch": 1767225600000.5,
                    "severityText": "Error",
                    "severityNumber": 17,
                    "body": "{\"orderId\":42}",
                    "traceId": "trace-1",
                    "spanId": "span-1",
                    "bodyDetail": {
                      "__typename": "OpenTelemetryLogBody",
                      "json": "{\"orderId\":42}",
                      "kind": "STRUCTURED",
                      "message": "order failed"
                    },
                    "logAttributes": [
                      { "__typename": "OpenTelemetryStringAttribute", "key": "customer", "string": "acme" },
                      { "__typename": "OpenTelemetryLongAttribute", "key": "orderId", "long": 9007199254740993 },
                      { "__typename": "OpenTelemetryFloatAttribute", "key": "ratio", "float": 0.75 },
                      { "__typename": "OpenTelemetryBoolAttribute", "key": "retried", "boolean": true }
                    ],
                    "resourceAttributes": [
                      { "__typename": "Attribute", "key": "service.name", "value": "orders" }
                    ],
                    "scope": {
                      "__typename": "OpenTelemetryScope",
                      "name": "Orders.Logging",
                      "schemaUrl": "https://opentelemetry.io/schemas/1.21.0",
                      "version": "1.2.0",
                      "attributes": [
                        { "__typename": "OpenTelemetryStringAttribute", "key": "library", "string": "serilog" },
                        { "__typename": "OpenTelemetryBoolAttribute", "key": "stable", "boolean": false }
                      ]
                    }
                  }
                }
              }
            }
            """);
        var client = api.CreateTelemetryClient();

        // act
        var log = await client.GetLogAsync(WorkspaceId, "log-1", TestContext.Current.CancellationToken);

        // assert
        log.MatchInlineSnapshot(
            """
            {
              "Id": "log-1",
              "Start": 1767225600000.5,
              "SeverityText": "Error",
              "SeverityNumber": 17,
              "Body": "{\"orderId\":42}",
              "TraceId": "trace-1",
              "SpanId": "span-1",
              "BodyDetail": {
                "Json": "{\"orderId\":42}",
                "Kind": "Structured",
                "Message": "order failed"
              },
              "LogAttributes": [
                {
                  "Key": "customer",
                  "Boolean": null,
                  "Float": null,
                  "Long": null,
                  "String": "acme"
                },
                {
                  "Key": "orderId",
                  "Boolean": null,
                  "Float": null,
                  "Long": 9007199254740993,
                  "String": null
                },
                {
                  "Key": "ratio",
                  "Boolean": null,
                  "Float": 0.75,
                  "Long": null,
                  "String": null
                },
                {
                  "Key": "retried",
                  "Boolean": true,
                  "Float": null,
                  "Long": null,
                  "String": null
                }
              ],
              "ResourceAttributes": [
                {
                  "Key": "service.name",
                  "Value": "orders"
                }
              ],
              "Scope": {
                "Name": "Orders.Logging",
                "SchemaUrl": "https://opentelemetry.io/schemas/1.21.0",
                "Version": "1.2.0",
                "Attributes": [
                  {
                    "Key": "library",
                    "Boolean": null,
                    "Float": null,
                    "Long": null,
                    "String": "serilog"
                  },
                  {
                    "Key": "stable",
                    "Boolean": false,
                    "Float": null,
                    "Long": null,
                    "String": null
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task GetLogAsync_Should_ReturnNullScope_When_LogHasNoScope()
    {
        // arrange
        var api = new FakeNitroApi(
            """
            {
              "data": {
                "workspaceById": {
                  "__typename": "Workspace",
                  "logById": {
                    "__typename": "OpenTelemetryLog",
                    "id": "log-1",
                    "epoch": 1767225600000,
                    "severityText": "Info",
                    "severityNumber": 9,
                    "body": "",
                    "traceId": "",
                    "spanId": "",
                    "bodyDetail": {
                      "__typename": "OpenTelemetryLogBody",
                      "json": null,
                      "kind": "EMPTY",
                      "message": null
                    },
                    "logAttributes": [],
                    "resourceAttributes": [],
                    "scope": null
                  }
                }
              }
            }
            """);
        var client = api.CreateTelemetryClient();

        // act
        var log = await client.GetLogAsync(WorkspaceId, "log-1", TestContext.Current.CancellationToken);

        // assert
        log.MatchInlineSnapshot(
            """
            {
              "Id": "log-1",
              "Start": 1767225600000.0,
              "SeverityText": "Info",
              "SeverityNumber": 9,
              "Body": "",
              "TraceId": "",
              "SpanId": "",
              "BodyDetail": {
                "Json": null,
                "Kind": "Empty",
                "Message": null
              },
              "LogAttributes": [],
              "ResourceAttributes": [],
              "Scope": null
            }
            """);
    }

    [Fact]
    public async Task ListServicesAsync_Should_MapEnvironmentsAndVersionMarkers_When_ResponseContainsServices()
    {
        // arrange
        // the second service has no version markers
        var api = new FakeNitroApi(
            $$"""
            {
              "data": {
                "workspaceById": {
                  "__typename": "Workspace",
                  "services": {
                    "__typename": "WorkspaceServicesConnection",
                    "edges": [
                      {
                        "__typename": "WorkspaceServicesEdge",
                        "cursor": "cursor-1",
                        "node": {
                          "__typename": "Service",
                          "name": "products",
                          "environmentNames": ["dev", "prod"],
                          "versionMarkers": [
                            {
                              "__typename": "ServiceVersionMarker",
                              "firstSeenAt": "2026-01-01T00:00:00.000Z",
                              "version": "1.0.0"
                            },
                            {
                              "__typename": "ServiceVersionMarker",
                              "firstSeenAt": "2026-01-01T00:30:00.000Z",
                              "version": "1.1.0"
                            }
                          ]
                        }
                      },
                      {
                        "__typename": "WorkspaceServicesEdge",
                        "cursor": "cursor-2",
                        "node": {
                          "__typename": "Service",
                          "name": "orders",
                          "environmentNames": [],
                          "versionMarkers": null
                        }
                      }
                    ],
                    "pageInfo": {{PageInfo}}
                  }
                }
              }
            }
            """);
        var client = api.CreateTelemetryClient();

        // act
        var page = await client.ListServicesAsync(
            WorkspaceId,
            null,
            null,
            null,
            s_from,
            s_to,
            null,
            null,
            TestContext.Current.CancellationToken);

        // assert
        page.MatchInlineSnapshot(
            """
            {
              "Items": [
                {
                  "Name": "products",
                  "EnvironmentNames": [
                    "dev",
                    "prod"
                  ],
                  "VersionMarkers": [
                    {
                      "FirstSeenAt": "2026-01-01T00:00:00+00:00",
                      "Version": "1.0.0"
                    },
                    {
                      "FirstSeenAt": "2026-01-01T00:30:00+00:00",
                      "Version": "1.1.0"
                    }
                  ]
                },
                {
                  "Name": "orders",
                  "EnvironmentNames": [],
                  "VersionMarkers": []
                }
              ],
              "EndCursor": "cursor-2",
              "HasNextPage": true
            }
            """);
    }

    [Fact]
    public async Task GetServiceAsync_Should_MapEnvironmentsAndVersionMarkers_When_ServiceIsFound()
    {
        // arrange
        var api = new FakeNitroApi(
            """
            {
              "data": {
                "workspaceById": {
                  "__typename": "Workspace",
                  "service": {
                    "__typename": "Service",
                    "name": "products",
                    "environmentNames": ["dev", "prod"],
                    "versionMarkers": [
                      {
                        "__typename": "ServiceVersionMarker",
                        "firstSeenAt": "2026-01-01T00:00:00.000Z",
                        "version": "1.0.0"
                      }
                    ]
                  }
                }
              }
            }
            """);
        var client = api.CreateTelemetryClient();

        // act
        var service = await client.GetServiceAsync(
            WorkspaceId,
            "products",
            null,
            s_from,
            s_to,
            TestContext.Current.CancellationToken);

        // assert
        service.MatchInlineSnapshot(
            """
            {
              "Name": "products",
              "EnvironmentNames": [
                "dev",
                "prod"
              ],
              "VersionMarkers": [
                {
                  "FirstSeenAt": "2026-01-01T00:00:00+00:00",
                  "Version": "1.0.0"
                }
              ]
            }
            """);
    }

    [Fact]
    public async Task GetServiceAsync_Should_ReturnNoVersionMarkers_When_ServerReturnsNull()
    {
        // arrange
        var api = new FakeNitroApi(
            """
            {
              "data": {
                "workspaceById": {
                  "__typename": "Workspace",
                  "service": {
                    "__typename": "Service",
                    "name": "products",
                    "environmentNames": ["dev"],
                    "versionMarkers": null
                  }
                }
              }
            }
            """);
        var client = api.CreateTelemetryClient();

        // act
        var service = await client.GetServiceAsync(
            WorkspaceId,
            "products",
            null,
            s_from,
            s_to,
            TestContext.Current.CancellationToken);

        // assert
        service.MatchInlineSnapshot(
            """
            {
              "Name": "products",
              "EnvironmentNames": [
                "dev"
              ],
              "VersionMarkers": []
            }
            """);
    }

    [Fact]
    public async Task ListAttributeKeysAsync_Should_MapKindAndPath_When_ResponseContainsKeys()
    {
        // arrange
        var api = new FakeNitroApi(
            $$"""
            {
              "data": {
                "workspaceById": {
                  "__typename": "Workspace",
                  "attributeKeys": {
                    "__typename": "WorkspaceAttributeKeysConnection",
                    "edges": [
                      {
                        "__typename": "WorkspaceAttributeKeysEdge",
                        "cursor": "cursor-1",
                        "node": {
                          "__typename": "OpenTelemetryAttributeKey",
                          "kind": "RESOURCE",
                          "path": "service.name"
                        }
                      },
                      {
                        "__typename": "WorkspaceAttributeKeysEdge",
                        "cursor": "cursor-2",
                        "node": {
                          "__typename": "OpenTelemetryAttributeKey",
                          "kind": "SPAN",
                          "path": "http.method"
                        }
                      }
                    ],
                    "pageInfo": {{PageInfo}}
                  }
                }
              }
            }
            """);
        var client = api.CreateTelemetryClient();

        // act
        var page = await client.ListAttributeKeysAsync(
            WorkspaceId,
            OpenTelemetrySignalKind.Traces,
            null,
            null,
            null,
            null,
            null,
            null,
            TestContext.Current.CancellationToken);

        // assert
        page.MatchInlineSnapshot(
            """
            {
              "Items": [
                {
                  "Kind": "Resource",
                  "Path": "service.name"
                },
                {
                  "Kind": "Span",
                  "Path": "http.method"
                }
              ],
              "EndCursor": "cursor-2",
              "HasNextPage": true
            }
            """);
    }

    [Fact]
    public async Task ListAttributeValuesAsync_Should_MapTypedValues_When_ResponseContainsEveryVariant()
    {
        // arrange
        var api = new FakeNitroApi(
            $$"""
            {
              "data": {
                "workspaceById": {
                  "__typename": "Workspace",
                  "attributeValues": {
                    "__typename": "WorkspaceAttributeValuesConnection",
                    "edges": [
                      {
                        "__typename": "WorkspaceAttributeValuesEdge",
                        "cursor": "cursor-1",
                        "node": {
                          "__typename": "OpenTelemetryAttributeValue",
                          "boolean": null,
                          "float": null,
                          "int": null,
                          "string": "GET"
                        }
                      },
                      {
                        "__typename": "WorkspaceAttributeValuesEdge",
                        "cursor": "cursor-2",
                        "node": {
                          "__typename": "OpenTelemetryAttributeValue",
                          "boolean": null,
                          "float": null,
                          "int": 200,
                          "string": null
                        }
                      },
                      {
                        "__typename": "WorkspaceAttributeValuesEdge",
                        "cursor": "cursor-3",
                        "node": {
                          "__typename": "OpenTelemetryAttributeValue",
                          "boolean": null,
                          "float": 0.75,
                          "int": null,
                          "string": null
                        }
                      },
                      {
                        "__typename": "WorkspaceAttributeValuesEdge",
                        "cursor": "cursor-4",
                        "node": {
                          "__typename": "OpenTelemetryAttributeValue",
                          "boolean": true,
                          "float": null,
                          "int": null,
                          "string": null
                        }
                      }
                    ],
                    "pageInfo": {{PageInfo}}
                  }
                }
              }
            }
            """);
        var client = api.CreateTelemetryClient();

        // act
        var page = await client.ListAttributeValuesAsync(
            WorkspaceId,
            OpenTelemetrySignalKind.Traces,
            "http.method",
            null,
            null,
            null,
            null,
            null,
            null,
            TestContext.Current.CancellationToken);

        // assert
        page.MatchInlineSnapshot(
            """
            {
              "Items": [
                {
                  "Boolean": null,
                  "Float": null,
                  "Int": null,
                  "String": "GET"
                },
                {
                  "Boolean": null,
                  "Float": null,
                  "Int": 200,
                  "String": null
                },
                {
                  "Boolean": null,
                  "Float": 0.75,
                  "Int": null,
                  "String": null
                },
                {
                  "Boolean": true,
                  "Float": null,
                  "Int": null,
                  "String": null
                }
              ],
              "EndCursor": "cursor-2",
              "HasNextPage": true
            }
            """);
    }

    private static string Span(string typeName, string spanId, string? extraFields = null)
        => $$"""
            {
              "__typename": "{{typeName}}",
              "spanId": "{{spanId}}",
              "parentSpanId": "parent",
              "spanName": "{{spanId}}-name",
              "spanKind": "INTERNAL",
              "duration": 1,
              "epoch": 1767225600000,
              "statusCode": "OK",
              "statusMessage": "",
              "resourceAttributes": [],
              "spanAttributes": [],
              "events": []{{(extraFields is null ? "" : "," + extraFields)}}
            }
            """;
}
