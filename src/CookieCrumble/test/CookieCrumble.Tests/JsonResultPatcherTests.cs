using System.Buffers;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CookieCrumble.HotChocolate;
using CookieCrumble.HotChocolate.Formatters;
using CookieCrumble.Xunit3;
using HotChocolate.Collections.Immutable;
using HotChocolate.Execution;
using HotChocolate.Text.Json;
using HotChocolate.Transport.Http;

namespace CookieCrumble;

public class JsonResultPatcherTests
{
    static JsonResultPatcherTests()
    {
        Snapshot.RegisterTestFramework(new Xunit3Framework());
    }

    [Fact]
    public void WriteResponse_Should_MergeObjects_When_PatchRepeatsAnObjectOfTheInitialData()
    {
        // arrange
        var patcher = new JsonResultPatcher(rejectOverlappingPositions: false);

        // act
        var result = Apply(
            patcher,
            """
            {"data":{"node":{"child":{"id":"1"}}},"pending":[{"id":"0","path":["node"]}],"hasNext":true}
            """,
            """
            {"incremental":[{"id":"0","data":{"child":{"name":"a"}}}],"completed":[{"id":"0"}],"hasNext":false}
            """);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "node": {
                  "child": {
                    "id": "1",
                    "name": "a"
                  }
                }
              }
            }
            """);
    }

    [Fact]
    public void WriteResponse_Should_MergeNestedObjects_When_PatchRepeatsSeveralLevels()
    {
        // arrange
        var patcher = new JsonResultPatcher(rejectOverlappingPositions: false);

        // act
        var result = Apply(
            patcher,
            """
            {"data":{"node":{"child":{"inner":{"a":1}}}},"pending":[{"id":"0","path":["node"]}],"hasNext":true}
            """,
            """
            {"incremental":[{"id":"0","data":{"child":{"inner":{"b":2},"c":3}}}],"completed":[{"id":"0"}]}
            """);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "node": {
                  "child": {
                    "inner": {
                      "a": 1,
                      "b": 2
                    },
                    "c": 3
                  }
                }
              }
            }
            """);
    }

    [Fact]
    public void WriteResponse_Should_ReplaceLeafWithLastValue_When_PatchRepeatsALeaf()
    {
        // arrange
        var patcher = new JsonResultPatcher(rejectOverlappingPositions: false);

        // act
        var result = Apply(
            patcher,
            """
            {"data":{"node":{"name":"first"}},"pending":[{"id":"0","path":["node"]}],"hasNext":true}
            """,
            """
            {"incremental":[{"id":"0","data":{"name":"second"}}],"completed":[{"id":"0"}]}
            """);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "node": {
                  "name": "second"
                }
              }
            }
            """);
    }

    [Fact]
    public void WriteResponse_Should_MergeListItems_When_PatchRepeatsAListOfTheSameLength()
    {
        // arrange
        var patcher = new JsonResultPatcher(rejectOverlappingPositions: false);

        // act
        var result = Apply(
            patcher,
            """
            {"data":{"node":{"friends":[{"id":"1"},{"id":"2"}]}},"pending":[{"id":"0","path":["node"]}]}
            """,
            """
            {"incremental":[{"id":"0","data":{"friends":[{"name":"a"},{"name":"b"}]}}],"completed":[{"id":"0"}]}
            """);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "node": {
                  "friends": [
                    {
                      "id": "1",
                      "name": "a"
                    },
                    {
                      "id": "2",
                      "name": "b"
                    }
                  ]
                }
              }
            }
            """);
    }

    [Fact]
    public void WriteResponse_Should_MergeNestedListItems_When_PatchRepeatsNestedListsOfTheSameLength()
    {
        // arrange
        var patcher = new JsonResultPatcher(rejectOverlappingPositions: false);

        // act
        var result = Apply(
            patcher,
            """
            {"data":{"node":{"rows":[[{"id":"1"}]]}},"pending":[{"id":"0","path":["node"]}]}
            """,
            """
            {"incremental":[{"id":"0","data":{"rows":[[{"name":"a"}]]}}],"completed":[{"id":"0"}]}
            """);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "node": {
                  "rows": [
                    [
                      {
                        "id": "1",
                        "name": "a"
                      }
                    ]
                  ]
                }
              }
            }
            """);
    }

    [Fact]
    public void WriteResponse_Should_ReplaceList_When_PatchRepeatsAListOfAnotherLength()
    {
        // arrange
        var patcher = new JsonResultPatcher(rejectOverlappingPositions: false);

        // act
        var result = Apply(
            patcher,
            """
            {"data":{"node":{"friends":[{"id":"1"}]}},"pending":[{"id":"0","path":["node"]}]}
            """,
            """
            {"incremental":[{"id":"0","data":{"friends":[{"name":"a"},{"name":"b"}]}}],"completed":[{"id":"0"}]}
            """);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "node": {
                  "friends": [
                    {
                      "name": "a"
                    },
                    {
                      "name": "b"
                    }
                  ]
                }
              }
            }
            """);
    }

    [Fact]
    public void WriteResponse_Should_Throw_When_PatchRepeatsAnObject()
    {
        // arrange
        var patcher = new JsonResultPatcher(rejectOverlappingPositions: true);

        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => Apply(
                patcher,
                """
                {"data":{"node":{"child":{"id":"1"}}},"pending":[{"id":"0","path":["node"]}],"hasNext":true}
                """,
                """
                {"incremental":[{"id":"0","data":{"child":{"name":"a"}}}],"completed":[{"id":"0"}]}
                """));

        // assert
        exception.Message.MatchInlineSnapshot(
            "Incremental payloads delivered a response position more than once: node.child (object)");
    }

    [Fact]
    public void WriteResponse_Should_Throw_When_PatchRepeatsAList()
    {
        // arrange
        var patcher = new JsonResultPatcher(rejectOverlappingPositions: true);

        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => Apply(
                patcher,
                """
                {"data":{"node":{"friends":[{"id":"1"}]}},"pending":[{"id":"0","path":["node"]}],"hasNext":true}
                """,
                """
                {"incremental":[{"id":"0","data":{"friends":[{"name":"a"}]}}],"completed":[{"id":"0"}]}
                """));

        // assert
        exception.Message.MatchInlineSnapshot(
            "Incremental payloads delivered a response position more than once: node.friends (list)");
    }

    [Fact]
    public void WriteResponse_Should_Throw_When_PatchRepeatsAListOfScalars()
    {
        // arrange
        var patcher = new JsonResultPatcher(rejectOverlappingPositions: true);

        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => Apply(
                patcher,
                """
                {"data":{"node":{"tags":["a","b"]}},"pending":[{"id":"0","path":["node"]}],"hasNext":true}
                """,
                """
                {"incremental":[{"id":"0","data":{"tags":["a","c"]}}],"completed":[{"id":"0"}]}
                """));

        // assert
        exception.Message.MatchInlineSnapshot(
            "Incremental payloads delivered a response position more than once: node.tags (list), node.tags[1] (leaf)");
    }

    [Fact]
    public void WriteResponse_Should_Throw_When_PatchRepeatsAListOfAnotherLength()
    {
        // arrange
        var patcher = new JsonResultPatcher(rejectOverlappingPositions: true);

        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => Apply(
                patcher,
                """
                {"data":{"node":{"friends":[{"id":"1"}]}},"pending":[{"id":"0","path":["node"]}],"hasNext":true}
                """,
                """
                {"incremental":[{"id":"0","data":{"friends":[{"name":"a"},{"name":"b"}]}}],"completed":[{"id":"0"}]}
                """));

        // assert
        exception.Message.MatchInlineSnapshot(
            "Incremental payloads delivered a response position more than once: node.friends (list)");
    }

    [Fact]
    public void WriteResponse_Should_Throw_When_PatchRepeatsANestedListOfAnotherLength()
    {
        // arrange
        var patcher = new JsonResultPatcher(rejectOverlappingPositions: true);

        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => Apply(
                patcher,
                """
                {"data":{"node":{"rows":[[{"id":"1"}]]}},"pending":[{"id":"0","path":["node"]}],"hasNext":true}
                """,
                """
                {"incremental":[{"id":"0","data":{"rows":[[{"name":"a"},{"name":"b"}]]}}],"completed":[{"id":"0"}]}
                """));

        // assert
        exception.Message.MatchInlineSnapshot(
            "Incremental payloads delivered a response position more than once: node.rows (list), node.rows[0] (list)");
    }

    [Fact]
    public void WriteResponse_Should_Throw_When_PatchReplacesNullWithAnObject()
    {
        // arrange
        var patcher = new JsonResultPatcher(rejectOverlappingPositions: true);

        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => Apply(
                patcher,
                """
                {"data":{"node":{"child":null}},"pending":[{"id":"0","path":["node"]}],"hasNext":true}
                """,
                """
                {"incremental":[{"id":"0","data":{"child":{"name":"a"}}}],"completed":[{"id":"0"}]}
                """));

        // assert
        exception.Message.MatchInlineSnapshot(
            "Incremental payloads delivered a response position more than once: node.child (leaf)");
    }

    [Fact]
    public void WriteResponse_Should_Throw_When_PatchReplacesAnObjectWithAScalar()
    {
        // arrange
        var patcher = new JsonResultPatcher(rejectOverlappingPositions: true);

        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => Apply(
                patcher,
                """
                {"data":{"node":{"child":{"id":"1"}}},"pending":[{"id":"0","path":["node"]}],"hasNext":true}
                """,
                """
                {"incremental":[{"id":"0","data":{"child":"a"}}],"completed":[{"id":"0"}]}
                """));

        // assert
        exception.Message.MatchInlineSnapshot(
            "Incremental payloads delivered a response position more than once: node.child (leaf)");
    }

    [Fact]
    public void WriteResponse_Should_Throw_When_PatchReplacesAListWithAnObject()
    {
        // arrange
        var patcher = new JsonResultPatcher(rejectOverlappingPositions: true);

        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => Apply(
                patcher,
                """
                {"data":{"node":{"friends":[{"id":"1"}]}},"pending":[{"id":"0","path":["node"]}],"hasNext":true}
                """,
                """
                {"incremental":[{"id":"0","data":{"friends":{"name":"a"}}}],"completed":[{"id":"0"}]}
                """));

        // assert
        exception.Message.MatchInlineSnapshot(
            "Incremental payloads delivered a response position more than once: node.friends (leaf)");
    }

    [Fact]
    public void WriteResponse_Should_Throw_When_PatchRepeatsALeaf()
    {
        // arrange
        var patcher = new JsonResultPatcher(rejectOverlappingPositions: true);

        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => Apply(
                patcher,
                """
                {"data":{"nodes":[{"name":"first"}]},"pending":[{"id":"0","path":["nodes",0]}],"hasNext":true}
                """,
                """
                {"incremental":[{"id":"0","data":{"name":"second"}}],"completed":[{"id":"0"}]}
                """));

        // assert
        exception.Message.MatchInlineSnapshot(
            "Incremental payloads delivered a response position more than once: nodes[0].name (leaf)");
    }

    [Fact]
    public void WriteResponse_Should_ReportEveryPosition_When_PatchesRepeatNestedPositions()
    {
        // arrange
        var patcher = new JsonResultPatcher(rejectOverlappingPositions: true);

        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => Apply(
                patcher,
                """
                {"data":{"node":{"child":{"id":"1"}}},"pending":[{"id":"0","path":[]}],"hasNext":true}
                """,
                """
                {"incremental":[{"id":"0","data":{"node":{"child":{"id":"1","name":"a"}}}}],"completed":[{"id":"0"}]}
                """));

        // assert
        exception.Message.MatchInlineSnapshot(
            "Incremental payloads delivered a response position more than once: "
            + "node (object), node.child (object), node.child.id (leaf)");
    }

    [Fact]
    public void WriteResponse_Should_MergeObjects_When_DeferredFieldsArriveWithSubPath()
    {
        // arrange
        var patcher = new JsonResultPatcher(rejectOverlappingPositions: true);

        // act
        var result = Apply(
            patcher,
            """
            {"data":{"node":{"child":{"id":"1"}}},"pending":[{"id":"0","path":["node"]}],"hasNext":true}
            """,
            """
            {"incremental":[{"id":"0","subPath":["child"],"data":{"name":"a"}}],"completed":[{"id":"0"}]}
            """);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "node": {
                  "child": {
                    "id": "1",
                    "name": "a"
                  }
                }
              }
            }
            """);
    }

    [Fact]
    public void WriteResponse_Should_AppendItems_When_StreamedItemsArriveInSeveralPayloads()
    {
        // arrange
        var patcher = new JsonResultPatcher(rejectOverlappingPositions: true);

        // act
        var result = Apply(
            patcher,
            """
            {"data":{"list":[1]},"pending":[{"id":"0","path":["list"]}],"hasNext":true}
            """,
            """
            {"incremental":[{"id":"0","items":[2]}],"hasNext":true}
            """,
            """
            {"incremental":[{"id":"0","items":[3]}],"completed":[{"id":"0"}],"hasNext":false}
            """);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "list": [
                  1,
                  2,
                  3
                ]
              }
            }
            """);
    }

    [Fact]
    public void Format_Should_Throw_When_ScopeRejectsOverlapsAndPatchRepeatsAnObject()
    {
        // arrange
        using var response = CreateJsonLinesResponse();
        using var scope = IncrementalDeliveryChecks.RejectOverlappingPositions();
        var buffer = new ArrayBufferWriter<byte>();

        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => SnapshotValueFormatters.GraphQLHttpStable.Format(buffer, response));

        // assert
        exception.Message.MatchInlineSnapshot(
            "Incremental payloads delivered a response position more than once: node.child (object)");
    }

    [Fact]
    public void Format_Should_MergeObjects_When_NoScopeRejectsOverlaps()
    {
        // act
        var result = FormatStable();

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "kind": "stable-stream",
              "payloadCount": 2,
              "initial": {
                "data": {
                  "node": {
                    "child": {
                      "id": "1"
                    }
                  }
                }
              },
              "pending": [
                {
                  "id": "0",
                  "path": [
                    "node"
                  ]
                }
              ],
              "incremental": [
                {
                  "id": "0",
                  "path": [
                    "node"
                  ],
                  "data": {
                    "child": {
                      "name": "a"
                    }
                  }
                }
              ],
              "completed": [
                {
                  "id": "0"
                }
              ],
              "final": {
                "data": {
                  "node": {
                    "child": {
                      "id": "1",
                      "name": "a"
                    }
                  }
                }
              }
            }
            """);
    }

    [Fact]
    public void Format_Should_MergeObjects_When_ScopeIsDisposed()
    {
        // arrange
        var expected = FormatStable();
        IncrementalDeliveryChecks.RejectOverlappingPositions().Dispose();

        // act
        var actual = FormatStable();

        // assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Format_Should_Throw_When_ScopeRejectsOverlapsAndDeferredResultOverlaps()
    {
        // arrange
        var stream = CreateDeferredResultStream();
        using var scope = IncrementalDeliveryChecks.RejectOverlappingPositions();
        var buffer = new ArrayBufferWriter<byte>();

        // act
        var exception = Assert.Throws<AggregateException>(
            () => SnapshotValueFormatters.ExecutionResult.Format(buffer, stream));

        // assert
        exception.InnerException!.Message.MatchInlineSnapshot(
            "Incremental payloads delivered a response position more than once: node.child (object)");
    }

    [Fact]
    public void Format_Should_Throw_When_ScopeRejectsOverlapsAndStableDeferredResultOverlaps()
    {
        // arrange
        var stream = CreateDeferredResultStream();
        using var scope = IncrementalDeliveryChecks.RejectOverlappingPositions();
        var buffer = new ArrayBufferWriter<byte>();

        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => SnapshotValueFormatters.ExecutionResultStable.Format(buffer, stream));

        // assert
        exception.Message.MatchInlineSnapshot(
            "Incremental payloads delivered a response position more than once: node.child (object)");
    }

    [Fact]
    public void Format_Should_Throw_When_ScopeRejectsOverlapsAndMultipartResponseOverlaps()
    {
        // arrange
        using var response = CreateMultipartResponse();
        using var scope = IncrementalDeliveryChecks.RejectOverlappingPositions();
        var buffer = new ArrayBufferWriter<byte>();

        // act
        var exception = Assert.Throws<AggregateException>(
            () => SnapshotValueFormatters.GraphQLHttp.Format(buffer, response));

        // assert
        exception.InnerException!.Message.MatchInlineSnapshot(
            "Incremental payloads delivered a response position more than once: node.child (object)");
    }

    [Fact]
    public void Format_Should_Throw_When_ScopeRejectsOverlapsAndStableMultipartResponseOverlaps()
    {
        // arrange
        using var response = CreateMultipartResponse();
        using var scope = IncrementalDeliveryChecks.RejectOverlappingPositions();
        var buffer = new ArrayBufferWriter<byte>();

        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => SnapshotValueFormatters.GraphQLHttpStable.Format(buffer, response));

        // assert
        exception.Message.MatchInlineSnapshot(
            "Incremental payloads delivered a response position more than once: node.child (object)");
    }

    [Fact]
    public void Format_Should_WriteEnvelope_When_NoScopeRejectsOverlapsAndDeferredResultOverlaps()
    {
        // arrange
        var stream = CreateDeferredResultStream();
        var buffer = new ArrayBufferWriter<byte>();

        // act
        SnapshotValueFormatters.ExecutionResult.Format(buffer, stream);

        // assert
        Encoding.UTF8.GetString(buffer.WrittenSpan).TrimEnd().MatchInlineSnapshot(
            """
            {
              "data": {
                "node": {
                  "child": {
                    "id": "1"
                  }
                }
              },
              "pending": [
                {
                  "id": "0",
                  "path": [
                    "node"
                  ]
                }
              ],
              "incremental": [
                {
                  "id": "0",
                  "data": {
                    "child": {
                      "name": "a"
                    }
                  }
                }
              ],
              "completed": [
                {
                  "id": "0"
                }
              ],
              "hasNext": false
            }
            """);
    }

    private static ResponseStream CreateDeferredResultStream()
    {
        var initial = new OperationResult(
            CreateData(
                new Dictionary<string, object?>
                {
                    ["node"] = new Dictionary<string, object?>
                    {
                        ["child"] = new Dictionary<string, object?> { ["id"] = "1" }
                    }
                }))
        {
            Pending = [new PendingResult(0, global::HotChocolate.Path.Root.Append("node"))],
            HasNext = true
        };

        var incremental = new OperationResult(
            ImmutableOrderedDictionary<string, object?>.Empty.Add("__placeholder", true))
        {
            Incremental = [
                new IncrementalObjectResult(
                    0,
                    data: CreateData(
                        new Dictionary<string, object?>
                        {
                            ["child"] = new Dictionary<string, object?> { ["name"] = "a" }
                        }))
            ],
            Completed = [new CompletedResult(0)],
            HasNext = false,
            Extensions = []
        };

        return new ResponseStream(() => ReadAsync(initial, incremental), ExecutionResultKind.DeferredResult);
    }

    private static async IAsyncEnumerable<OperationResult> ReadAsync(params OperationResult[] results)
    {
        foreach (var result in results)
        {
            yield return result;
            await Task.Yield();
        }
    }

    private static OperationResultData CreateData(object value)
        => new(value, isValueNull: false, new ValueFormatter(value), memoryHolder: null);

    private static GraphQLHttpResponse CreateMultipartResponse()
    {
        const string body =
            "---\r\n"
            + "Content-Type: application/json\r\n\r\n"
            + "{\"data\":{\"node\":{\"child\":{\"id\":\"1\"}}},"
            + "\"pending\":[{\"id\":\"0\",\"path\":[\"node\"]}],\"hasNext\":true}\r\n"
            + "---\r\n"
            + "Content-Type: application/json\r\n\r\n"
            + "{\"incremental\":[{\"id\":\"0\",\"data\":{\"child\":{\"name\":\"a\"}}}],"
            + "\"completed\":[{\"id\":\"0\"}],\"hasNext\":false}\r\n"
            + "-----\r\n";

        var message = new HttpResponseMessage { Content = new StringContent(body, Encoding.UTF8) };
        message.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/mixed; boundary=\"-\"");
        return new GraphQLHttpResponse(message);
    }

    private static string FormatStable()
    {
        using var response = CreateJsonLinesResponse();
        var buffer = new ArrayBufferWriter<byte>();
        SnapshotValueFormatters.GraphQLHttpStable.Format(buffer, response);
        return Encoding.UTF8.GetString(buffer.WrittenSpan).TrimEnd();
    }

    private static GraphQLHttpResponse CreateJsonLinesResponse()
    {
        const string body =
            """
            {"data":{"node":{"child":{"id":"1"}}},"pending":[{"id":"0","path":["node"]}],"hasNext":true}
            {"incremental":[{"id":"0","data":{"child":{"name":"a"}}}],"completed":[{"id":"0"}],"hasNext":false}
            """;

        var message = new HttpResponseMessage { Content = new StringContent(body, Encoding.UTF8) };
        message.Content.Headers.ContentType = new MediaTypeHeaderValue("application/jsonl");
        return new GraphQLHttpResponse(message);
    }

    private static string Apply(JsonResultPatcher patcher, string initial, params string[] patches)
    {
        var documents = new List<JsonDocument>();

        try
        {
            var document = JsonDocument.Parse(initial);
            documents.Add(document);
            patcher.SetResponse(document);

            foreach (var patch in patches)
            {
                document = JsonDocument.Parse(patch);
                documents.Add(document);
                patcher.ApplyPatch(document);
            }

            var buffer = new ArrayBufferWriter<byte>();
            patcher.WriteResponse(buffer);
            return Encoding.UTF8.GetString(buffer.WrittenSpan);
        }
        finally
        {
            foreach (var document in documents)
            {
                document.Dispose();
            }
        }
    }

    private sealed class ValueFormatter(object value) : IRawJsonFormatter
    {
        public void WriteDataTo(JsonWriter jsonWriter)
            => JsonValueFormatter.WriteValue(jsonWriter, value, new JsonSerializerOptions());
    }
}
