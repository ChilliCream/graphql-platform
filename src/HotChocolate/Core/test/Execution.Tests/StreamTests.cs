using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text.Json;
using GreenDonut;
using GreenDonut.Data;
using HotChocolate.Execution.Configuration;
using HotChocolate.Execution.DependencyInjection;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Options;
using HotChocolate.Execution.Processing;
using HotChocolate.Execution.Processing.Tasks;
using HotChocolate.Text.Json;
using HotChocolate.Types;
using HotChocolate.Types.Pagination;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Execution;

public class StreamTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(10);

    /// <summary>
    /// This test shows how IAsyncEnumerable is translated to SDL
    /// </summary>
    [Fact]
    public async Task Schema()
    {
        var executor = await DeferAndStreamTestSchema.CreateAsync();
        executor.Schema.MatchSnapshot();
    }

    [Fact]
    public async Task Stream()
    {
        // arrange
        var executor = await DeferAndStreamTestSchema.CreateAsync();

        // act
        var result = await executor.ExecuteAsync(
            @"{
                ... @defer {
                    wait(m: 300)
                }
                persons @stream {
                    id
                }
            }",
            TestContext.Current.CancellationToken);

        Assert.IsType<ResponseStream>(result).MatchSnapshot();
    }

    [Fact]
    public async Task Stream_Nested_Defer()
    {
        // arrange
        var executor = await DeferAndStreamTestSchema.CreateAsync();

        // act
        var result = await executor.ExecuteAsync(
            @"{
                ... @defer {
                    wait(m: 800)
                }
                personNodes(first: 1) {
                    nodes @stream {
                        ... @defer {
                            name
                        }
                    }
                }
            }",
            TestContext.Current.CancellationToken);

        Assert.IsType<ResponseStream>(result).MatchSnapshot();
    }

    [Fact]
    public async Task Stream_InitialCount_Set_To_1()
    {
        // arrange
        var executor = await DeferAndStreamTestSchema.CreateAsync();

        // act
        var result = await executor.ExecuteAsync(
            @"{
                ... @defer {
                    wait(m: 300)
                }
                persons @stream(initialCount: 1) {
                    id
                }
            }",
            TestContext.Current.CancellationToken);

        Assert.IsType<ResponseStream>(result).MatchSnapshot();
    }

    [Fact]
    public async Task Stream_InitialCount_Exceeds_Total_Count()
    {
        // arrange
        var executor = await DeferAndStreamTestSchema.CreateAsync();

        // act
        var result = await executor.ExecuteAsync(
            @"{
                ... @defer {
                    wait(m: 300)
                }
                persons @stream(initialCount: 7) {
                    id
                }
            }",
            TestContext.Current.CancellationToken);

        Assert.IsType<ResponseStream>(result).MatchSnapshot();
    }

    [Fact]
    public async Task Stream_Label_Set_To_abc()
    {
        // arrange
        var executor = await DeferAndStreamTestSchema.CreateAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                ... @defer {
                    wait(m: 300)
                }
                persons @stream(label: "abc") {
                    id
                }
            }
            """,
            TestContext.Current.CancellationToken);

        Assert.IsType<ResponseStream>(result).MatchSnapshot();
    }

    [Fact]
    public async Task Stream_If_Set_To_false()
    {
        // arrange
        var executor = await DeferAndStreamTestSchema.CreateAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                persons @stream(if: false) {
                    id
                }
            }
            """,
            TestContext.Current.CancellationToken);

        Assert.IsType<OperationResult>(result).MatchSnapshot();
    }

    [Fact]
    public async Task Stream_If_Variable_Set_To_false()
    {
        // arrange
        var executor = await DeferAndStreamTestSchema.CreateAsync();

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder
                .New()
                .SetDocument(
                    """
                    query ($stream: Boolean!) {
                        persons @stream(if: $stream) {
                            id
                        }
                    }
                    """)
                .SetVariableValues(new Dictionary<string, object?> { { "stream", false } })
                .Build(),
            TestContext.Current.CancellationToken);

        Assert.IsType<OperationResult>(result).MatchSnapshot();
    }

    [Fact]
    public async Task Stream_Should_BeRejected_When_IfIsNull()
    {
        // arrange
        var executor = await DeferAndStreamTestSchema.CreateAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                persons @stream(if: null) {
                    id
                }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        Assert.IsType<OperationResult>(result).MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "The specified argument value does not match the argument type.",
                  "locations": [
                    {
                      "line": 2,
                      "column": 25
                    }
                  ],
                  "path": [
                    "persons"
                  ],
                  "extensions": {
                    "argument": "if",
                    "argumentValue": "null",
                    "locationType": "Boolean!",
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Values-of-Correct-Type"
                  }
                }
              ]
            }
            """);
    }

    [Fact]
    public async Task Stream_Should_Stream_When_NullableIfVariableIsNotProvided()
    {
        // arrange
        var executor = await CreateExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            "query ($stream: Boolean) { items(count: 2) @stream(initialCount: 1, if: $stream) { id } }",
            TestContext.Current.CancellationToken);

        // assert
        Assert.IsType<ResponseStream>(result).MatchInlineSnapshot(
            """
            {
              "data": {
                "items": [
                  {
                    "id": 1
                  }
                ]
              },
              "pending": [
                {
                  "id": "2",
                  "path": [
                    "items"
                  ]
                }
              ],
              "incremental": [
                {
                  "id": "2",
                  "items": [
                    {
                      "id": 2
                    }
                  ]
                }
              ],
              "completed": [
                {
                  "id": "2"
                }
              ],
              "hasNext": false
            }

            """);
    }

    [Fact]
    public async Task Stream_Should_BeRejected_When_NullableIfVariableIsNull()
    {
        // arrange
        var executor = await CreateExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder
                .New()
                .SetDocument(
                    "query ($stream: Boolean) { items(count: 2) @stream(initialCount: 1, if: $stream) { id } }")
                .SetVariableValues(new Dictionary<string, object?> { { "stream", null } })
                .Build(),
            TestContext.Current.CancellationToken);

        // assert
        Assert.IsType<OperationResult>(result).MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "The variable `stream` is null, but the `if` argument of the `@stream` directive must not be null.",
                  "path": [
                    "items"
                  ],
                  "extensions": {
                    "code": "HC0018",
                    "variable": "stream",
                    "directive": "stream"
                  }
                }
              ],
              "data": null
            }
            """);
    }

    [Fact]
    public async Task AsyncEnumerable_Result()
    {
        // arrange
        var executor = await DeferAndStreamTestSchema.CreateAsync();

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder
                .New()
                .SetDocument(
                    """
                    {
                        persons {
                            id
                        }
                    }
                    """)
                .Build(),
            TestContext.Current.CancellationToken);

        Assert.IsType<OperationResult>(result).MatchSnapshot();
    }

    [Fact]
    public async Task Stream_Should_BeIgnored_When_FieldIsConnectionNodes()
    {
        // arrange
        var executor = await DeferAndStreamTestSchema.CreateAsync();

        // act
        var result = await executor.ExecuteAsync(
            "{ personNodes(first: 2) { nodes @stream(initialCount: 0) { id } } }",
            TestContext.Current.CancellationToken);

        // assert
        Assert.IsType<OperationResult>(result).MatchInlineSnapshot(
            """
            {
              "data": {
                "personNodes": {
                  "nodes": [
                    {
                      "id": "UGVyc29uOjE="
                    },
                    {
                      "id": "UGVyc29uOjI="
                    }
                  ]
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Stream_Should_BeIgnored_When_FieldIsConnectionEdges()
    {
        // arrange
        var executor = await DeferAndStreamTestSchema.CreateAsync();

        // act
        var result = await executor.ExecuteAsync(
            "{ personNodes(first: 2) { edges @stream(initialCount: 0) { node { id } } } }",
            TestContext.Current.CancellationToken);

        // assert
        Assert.IsType<OperationResult>(result).MatchInlineSnapshot(
            """
            {
              "data": {
                "personNodes": {
                  "edges": [
                    {
                      "node": {
                        "id": "UGVyc29uOjE="
                      }
                    },
                    {
                      "node": {
                        "id": "UGVyc29uOjI="
                      }
                    }
                  ]
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Stream_Should_BeIgnored_When_FieldIsPageConnectionNodesOrEdges()
    {
        // arrange
        var executor = await CreateExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                pagedItems {
                    nodes @stream(initialCount: 0) { id }
                    edges @stream(initialCount: 0) { node { id } }
                }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        Assert.IsType<OperationResult>(result).MatchInlineSnapshot(
            """
            {
              "data": {
                "pagedItems": {
                  "nodes": [
                    {
                      "id": 1
                    },
                    {
                      "id": 2
                    }
                  ],
                  "edges": [
                    {
                      "node": {
                        "id": 1
                      }
                    },
                    {
                      "node": {
                        "id": 2
                      }
                    }
                  ]
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Stream_Should_BeIgnored_When_AsyncFieldIsFlaggedAsConnectionNodesOrEdges()
    {
        // arrange
        var executor = await CreateExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                connectionLike {
                    nodes @stream(initialCount: 0) { id }
                    edges @stream(initialCount: 0) { id }
                }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        Assert.IsType<OperationResult>(result).MatchInlineSnapshot(
            """
            {
              "data": {
                "connectionLike": {
                  "nodes": [
                    {
                      "id": 1
                    },
                    {
                      "id": 2
                    }
                  ],
                  "edges": [
                    {
                      "id": 1
                    },
                    {
                      "id": 2
                    }
                  ]
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Stream_Should_DeliverBufferedItemsInOnePayload_When_SourceYieldsSynchronously()
    {
        // arrange
        var state = new StreamState();
        state.ReleaseHeld(1);
        var executor = await CreateExecutorAsync(state);

        // act
        var payloads = await ReadPayloadsAsync(
            executor,
            "{ items(count: 4) @stream(initialCount: 1) { id held } }",
            () =>
            {
                state.ReleaseHeld(2);
                state.ReleaseHeld(3);
                state.ReleaseHeld(4);
            });

        // assert
        payloads.MatchInlineSnapshots(
        [
            """
            {
              "data": {
                "items": [
                  {
                    "id": 1,
                    "held": 1
                  }
                ]
              },
              "pending": [
                {
                  "id": "2",
                  "path": [
                    "items"
                  ]
                }
              ],
              "hasNext": true
            }
            """,
            """
            {
              "incremental": [
                {
                  "id": "2",
                  "items": [
                    {
                      "id": 2,
                      "held": 2
                    },
                    {
                      "id": 3,
                      "held": 3
                    },
                    {
                      "id": 4,
                      "held": 4
                    }
                  ]
                }
              ],
              "completed": [
                {
                  "id": "2"
                }
              ],
              "hasNext": false
            }
            """
        ]);
    }

    [Fact]
    public async Task Stream_Should_DeliverOnePayloadPerItem_When_SourceWaitsBeforeEveryItem()
    {
        // arrange
        var state = new StreamState();
        var executor = await CreateExecutorAsync(state);

        // act
        var payloads = await ReadPayloadsAsync(
            executor,
            "{ gatedItems @stream(initialCount: 1) { id } }",
            state.ReleaseNextItem,
            state.ReleaseNextItem,
            state.ReleaseNextItem);

        // assert
        payloads.MatchInlineSnapshots(
        [
            """
            {
              "data": {
                "gatedItems": [
                  {
                    "id": 1
                  }
                ]
              },
              "pending": [
                {
                  "id": "2",
                  "path": [
                    "gatedItems"
                  ]
                }
              ],
              "hasNext": true
            }
            """,
            """
            {
              "incremental": [
                {
                  "id": "2",
                  "items": [
                    {
                      "id": 2
                    }
                  ]
                }
              ],
              "hasNext": true
            }
            """,
            """
            {
              "incremental": [
                {
                  "id": "2",
                  "items": [
                    {
                      "id": 3
                    }
                  ]
                }
              ],
              "hasNext": true
            }
            """,
            """
            {
              "completed": [
                {
                  "id": "2"
                }
              ],
              "hasNext": false
            }
            """
        ]);
    }

    [Fact]
    public async Task Stream_Should_BatchChildResolvers_When_ItemsAreBufferedTogether()
    {
        // arrange
        var state = new StreamState();
        state.ReleaseHeld(1);
        var executor = await CreateExecutorAsync(state);

        // act
        var payloads = await ReadPayloadsAsync(
            executor,
            "{ items(count: 5) @stream(initialCount: 1) { id loadedAfterFirst held } }",
            () =>
            {
                state.ReleaseHeld(2);
                state.ReleaseHeld(3);
                state.ReleaseHeld(4);
                state.ReleaseHeld(5);
            });

        // assert
        Assert.Equal(["2,3,4,5"], state.Batches);
        payloads.MatchInlineSnapshots(
        [
            """
            {
              "data": {
                "items": [
                  {
                    "id": 1,
                    "loadedAfterFirst": "initial",
                    "held": 1
                  }
                ]
              },
              "pending": [
                {
                  "id": "2",
                  "path": [
                    "items"
                  ]
                }
              ],
              "hasNext": true
            }
            """,
            """
            {
              "incremental": [
                {
                  "id": "2",
                  "items": [
                    {
                      "id": 2,
                      "loadedAfterFirst": "loaded-2",
                      "held": 2
                    },
                    {
                      "id": 3,
                      "loadedAfterFirst": "loaded-3",
                      "held": 3
                    },
                    {
                      "id": 4,
                      "loadedAfterFirst": "loaded-4",
                      "held": 4
                    },
                    {
                      "id": 5,
                      "loadedAfterFirst": "loaded-5",
                      "held": 5
                    }
                  ]
                }
              ],
              "completed": [
                {
                  "id": "2"
                }
              ],
              "hasNext": false
            }
            """
        ]);
    }

    [Fact]
    public async Task Stream_Should_ExecuteNestedDefer_When_ItemsDeferFields()
    {
        // arrange
        var state = new StreamState();
        var executor = await CreateExecutorAsync(state);

        // act
        var payloads = await ReadPayloadsAsync(
            executor,
            """
            {
                burstItems(count: 2) @stream(initialCount: 1) {
                    id
                    ... @defer {
                        gatedName
                    }
                }
            }
            """,
            state.ReleaseNextItem,
            state.ReleaseNextItem,
            () => state.ReleaseName(1),
            () => state.ReleaseName(2));

        // assert
        payloads.MatchInlineSnapshots(
        [
            """
            {
              "data": {
                "burstItems": [
                  {
                    "id": 1
                  }
                ]
              },
              "pending": [
                {
                  "id": "2",
                  "path": [
                    "burstItems",
                    0
                  ]
                },
                {
                  "id": "3",
                  "path": [
                    "burstItems"
                  ]
                }
              ],
              "hasNext": true
            }
            """,
            """
            {
              "pending": [
                {
                  "id": "5",
                  "path": [
                    "burstItems",
                    1
                  ]
                }
              ],
              "incremental": [
                {
                  "id": "3",
                  "items": [
                    {
                      "id": 2
                    }
                  ]
                }
              ],
              "hasNext": true
            }
            """,
            """
            {
              "completed": [
                {
                  "id": "3"
                }
              ],
              "hasNext": true
            }
            """,
            """
            {
              "incremental": [
                {
                  "id": "2",
                  "data": {
                    "gatedName": "item-1"
                  }
                }
              ],
              "completed": [
                {
                  "id": "2"
                }
              ],
              "hasNext": true
            }
            """,
            """
            {
              "incremental": [
                {
                  "id": "5",
                  "data": {
                    "gatedName": "item-2"
                  }
                }
              ],
              "completed": [
                {
                  "id": "5"
                }
              ],
              "hasNext": false
            }
            """
        ]);
    }

    [Fact]
    public async Task Stream_Should_Stream_When_ListIsInsideDeferredFragment()
    {
        // arrange
        var executor = await CreateExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            "{ ... @defer { items(count: 3) @stream(initialCount: 1) { id } } }",
            TestContext.Current.CancellationToken);

        // assert
        Assert.IsType<ResponseStream>(result).MatchInlineSnapshot(
            """
            {
              "data": {},
              "pending": [
                {
                  "id": "2",
                  "path": []
                },
                {
                  "id": "3",
                  "path": [
                    "items"
                  ]
                }
              ],
              "incremental": [
                {
                  "id": "2",
                  "data": {
                    "items": [
                      {
                        "id": 1
                      }
                    ]
                  }
                },
                {
                  "id": "3",
                  "items": [
                    {
                      "id": 2
                    },
                    {
                      "id": 3
                    }
                  ]
                }
              ],
              "completed": [
                {
                  "id": "2"
                },
                {
                  "id": "3"
                }
              ],
              "hasNext": false
            }

            """);
    }

    [Fact]
    public async Task Stream_Should_StreamEachList_When_ListsAreInsideListItems()
    {
        // arrange
        var state = new StreamState();
        var executor = await CreateExecutorAsync(state);

        // act
        var payloads = await ReadPayloadsAsync(
            executor,
            "{ items(count: 2) { id gatedChildren @stream(initialCount: 1) { id } } }",
            () => state.ReleaseChildren(1),
            () => state.ReleaseChildrenEnd(1),
            () => state.ReleaseChildren(2),
            () => state.ReleaseChildrenEnd(2));

        // assert
        payloads.MatchInlineSnapshots(
        [
            """
            {
              "data": {
                "items": [
                  {
                    "id": 1,
                    "gatedChildren": [
                      {
                        "id": 11
                      }
                    ]
                  },
                  {
                    "id": 2,
                    "gatedChildren": [
                      {
                        "id": 21
                      }
                    ]
                  }
                ]
              },
              "pending": [
                {
                  "id": "2",
                  "path": [
                    "items",
                    0,
                    "gatedChildren"
                  ]
                },
                {
                  "id": "3",
                  "path": [
                    "items",
                    1,
                    "gatedChildren"
                  ]
                }
              ],
              "hasNext": true
            }
            """,
            """
            {
              "incremental": [
                {
                  "id": "2",
                  "items": [
                    {
                      "id": 12
                    }
                  ]
                }
              ],
              "hasNext": true
            }
            """,
            """
            {
              "completed": [
                {
                  "id": "2"
                }
              ],
              "hasNext": true
            }
            """,
            """
            {
              "incremental": [
                {
                  "id": "3",
                  "items": [
                    {
                      "id": 22
                    }
                  ]
                }
              ],
              "hasNext": true
            }
            """,
            """
            {
              "completed": [
                {
                  "id": "3"
                }
              ],
              "hasNext": false
            }
            """
        ]);
    }

    [Fact]
    public async Task Stream_Should_StreamNestedLists_When_StreamedItemsStreamTheirChildren()
    {
        // arrange
        var state = new StreamState();
        var executor = await CreateExecutorAsync(state);

        // act
        var payloads = await ReadPayloadsAsync(
            executor,
            "{ burstItems(count: 2) @stream(initialCount: 1) { id gatedChildren @stream(initialCount: 1) { id } } }",
            state.ReleaseNextItem,
            state.ReleaseNextItem,
            () => state.ReleaseChildren(1),
            () => state.ReleaseChildrenEnd(1),
            () => state.ReleaseChildren(2),
            () => state.ReleaseChildrenEnd(2));

        // assert
        payloads.MatchInlineSnapshots(
        [
            """
            {
              "data": {
                "burstItems": [
                  {
                    "id": 1,
                    "gatedChildren": [
                      {
                        "id": 11
                      }
                    ]
                  }
                ]
              },
              "pending": [
                {
                  "id": "2",
                  "path": [
                    "burstItems"
                  ]
                },
                {
                  "id": "3",
                  "path": [
                    "burstItems",
                    0,
                    "gatedChildren"
                  ]
                }
              ],
              "hasNext": true
            }
            """,
            """
            {
              "pending": [
                {
                  "id": "5",
                  "path": [
                    "burstItems",
                    1,
                    "gatedChildren"
                  ]
                }
              ],
              "incremental": [
                {
                  "id": "2",
                  "items": [
                    {
                      "id": 2,
                      "gatedChildren": [
                        {
                          "id": 21
                        }
                      ]
                    }
                  ]
                }
              ],
              "hasNext": true
            }
            """,
            """
            {
              "completed": [
                {
                  "id": "2"
                }
              ],
              "hasNext": true
            }
            """,
            """
            {
              "incremental": [
                {
                  "id": "3",
                  "items": [
                    {
                      "id": 12
                    }
                  ]
                }
              ],
              "hasNext": true
            }
            """,
            """
            {
              "completed": [
                {
                  "id": "3"
                }
              ],
              "hasNext": true
            }
            """,
            """
            {
              "incremental": [
                {
                  "id": "5",
                  "items": [
                    {
                      "id": 22
                    }
                  ]
                }
              ],
              "hasNext": true
            }
            """,
            """
            {
              "completed": [
                {
                  "id": "5"
                }
              ],
              "hasNext": false
            }
            """
        ]);
    }

    [Fact]
    public async Task Stream_Should_DeliverScalarItems_When_ListHasLeafElements()
    {
        // arrange
        var state = new StreamState();
        var executor = await CreateExecutorAsync(state);

        // act
        var payloads = await ReadPayloadsAsync(
            executor,
            "{ burstNames @stream(initialCount: 1) }",
            state.ReleaseNextItem,
            state.ReleaseNextItem);

        // assert
        payloads.MatchInlineSnapshots(
        [
            """
            {
              "data": {
                "burstNames": [
                  "a"
                ]
              },
              "pending": [
                {
                  "id": "2",
                  "path": [
                    "burstNames"
                  ]
                }
              ],
              "hasNext": true
            }
            """,
            """
            {
              "incremental": [
                {
                  "id": "2",
                  "items": [
                    "b"
                  ]
                }
              ],
              "hasNext": true
            }
            """,
            """
            {
              "completed": [
                {
                  "id": "2"
                }
              ],
              "hasNext": false
            }
            """
        ]);
    }

    [Fact]
    public async Task Stream_Should_ReportItemErrorWithItemPath_When_ItemFieldIsNullable()
    {
        // arrange
        var executor = await CreateExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            "{ items(count: 4) @stream(initialCount: 1) { id flaky } }",
            TestContext.Current.CancellationToken);

        // assert
        Assert.IsType<ResponseStream>(result).MatchInlineSnapshot(
            """
            {
              "data": {
                "items": [
                  {
                    "id": 1,
                    "flaky": "ok-1"
                  }
                ]
              },
              "pending": [
                {
                  "id": "2",
                  "path": [
                    "items"
                  ]
                }
              ],
              "incremental": [
                {
                  "id": "2",
                  "items": [
                    {
                      "id": 2,
                      "flaky": "ok-2"
                    },
                    {
                      "id": 3,
                      "flaky": null
                    },
                    {
                      "id": 4,
                      "flaky": "ok-4"
                    }
                  ],
                  "errors": [
                    {
                      "message": "flaky item",
                      "path": [
                        "items",
                        2,
                        "flaky"
                      ]
                    }
                  ]
                }
              ],
              "completed": [
                {
                  "id": "2"
                }
              ],
              "hasNext": false
            }

            """);
    }

    [Fact]
    public async Task Stream_Should_ReportOverallItemIndex_When_ItemFailsAfterInitialCountOfTwo()
    {
        // arrange
        var executor = await CreateExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            "{ items(count: 5) @stream(initialCount: 2) { id flaky } }",
            TestContext.Current.CancellationToken);

        // assert
        var paths = await ReadErrorPathsAsync(Assert.IsType<ResponseStream>(result));
        paths.MatchInlineSnapshot(
            """
            [
              "[\"items\",2,\"flaky\"]"
            ]
            """);
    }

    [Fact]
    public async Task Stream_Should_ContinueIndexNumbering_When_EveryBatchHasOneItem()
    {
        // arrange
        var state = new StreamState();
        var executor = await CreateExecutorAsync(state);

        // act
        var result = await executor.ExecuteAsync(
            "{ gatedItems(count: 4) @stream(initialCount: 1) { id flaky } }",
            TestContext.Current.CancellationToken);

        var paths = new List<string>();

        await foreach (var payload in Assert.IsType<ResponseStream>(result)
            .ReadResultsAsync()
            .WithCancellation(TestContext.Current.CancellationToken))
        {
            await using var cleanup = payload;
            paths.AddRange(GetErrorPaths(payload.ToJson()));
            state.ReleaseNextItem();
        }

        // assert
        paths.MatchInlineSnapshot(
            """
            [
              "[\"gatedItems\",2,\"flaky\"]"
            ]
            """);
    }

    [Fact]
    public async Task Stream_Should_AnnounceNestedDeferWithOverallItemIndex_When_EveryBatchHasOneItem()
    {
        // arrange
        var state = new StreamState();
        var executor = await CreateExecutorAsync(state);
        state.ReleaseName(1);
        state.ReleaseName(2);
        state.ReleaseName(3);

        // act
        var payloads = await ReadPayloadsAsync(
            executor,
            "{ gatedItems(count: 3) @stream(initialCount: 1) { id ... @defer { gatedName } } }",
            state.ReleaseNextItem,
            state.ReleaseNextItem,
            state.ReleaseNextItem);

        // assert
        GetPendingPaths(payloads).MatchInlineSnapshot(
            """
            [
              "[\"gatedItems\",0]",
              "[\"gatedItems\",1]",
              "[\"gatedItems\",2]",
              "[\"gatedItems\"]"
            ]
            """);
    }

    [Fact]
    public async Task Stream_Should_AnnounceNestedStreamWithOverallItemIndex_When_StreamedItemStreamsItsChildren()
    {
        // arrange
        var state = new StreamState();
        var executor = await CreateExecutorAsync(state);
        state.ReleaseNextItem();
        state.ReleaseNextItem();
        state.ReleaseNextItem();

        // act
        var payloads = await ReadPayloadsAsync(
            executor,
            "{ gatedItems(count: 3) @stream(initialCount: 1) { id gatedChildren @stream(initialCount: 1) { id } } }",
            () => state.ReleaseChildren(2),
            () => state.ReleaseChildren(3),
            () => state.ReleaseChildren(1),
            () => state.ReleaseChildrenEnd(1),
            () => state.ReleaseChildrenEnd(2),
            () => state.ReleaseChildrenEnd(3));

        // assert
        GetPendingPaths(payloads).MatchInlineSnapshot(
            """
            [
              "[\"gatedItems\",0,\"gatedChildren\"]",
              "[\"gatedItems\",1,\"gatedChildren\"]",
              "[\"gatedItems\",2,\"gatedChildren\"]",
              "[\"gatedItems\"]"
            ]
            """);
    }

    [Fact]
    public async Task Stream_Should_ContinueIndexNumbering_When_SeveralBatchesHoldMoreThanOneItem()
    {
        // arrange
        // item 2 holds the first batch while the source fills the buffer, so the next batches hold two items each.
        var state = new StreamState();
        var executor = await CreateExecutorAsync(state, maxStreamBatchSize: 2);

        for (var id = 1; id <= 6; id++)
        {
            if (id != 2)
            {
                state.ReleaseName(id);
            }
        }

        // act
        var payloads = await ReadPayloadsWithStepsAsync(
            executor,
            "{ gatedItems(count: 6) @stream(initialCount: 1) { id gatedName flakyFromThree } }",
            [
                async () =>
                {
                    state.ReleaseNextItem();
                    await state.WaitForNameStartedAsync(2);

                    for (var i = 0; i < 4; i++)
                    {
                        state.ReleaseNextItem();
                    }

                    await WaitUntilAsync(() => state.Produced >= 6);
                    state.ReleaseName(2);
                },
                () => Task.CompletedTask,
                () => Task.CompletedTask,
                () =>
                {
                    state.ReleaseNextItem();
                    return Task.CompletedTask;
                }
            ]);

        // assert
        payloads.MatchInlineSnapshots(
        [
            """
            {
              "data": {
                "gatedItems": [
                  {
                    "id": 1,
                    "gatedName": "item-1",
                    "flakyFromThree": "ok-1"
                  }
                ]
              },
              "pending": [
                {
                  "id": "2",
                  "path": [
                    "gatedItems"
                  ]
                }
              ],
              "hasNext": true
            }
            """,
            """
            {
              "incremental": [
                {
                  "id": "2",
                  "items": [
                    {
                      "id": 2,
                      "gatedName": "item-2",
                      "flakyFromThree": "ok-2"
                    }
                  ]
                }
              ],
              "hasNext": true
            }
            """,
            """
            {
              "incremental": [
                {
                  "id": "2",
                  "errors": [
                    {
                      "message": "flaky item 3",
                      "path": [
                        "gatedItems",
                        2,
                        "flakyFromThree"
                      ]
                    },
                    {
                      "message": "flaky item 4",
                      "path": [
                        "gatedItems",
                        3,
                        "flakyFromThree"
                      ]
                    }
                  ],
                  "items": [
                    {
                      "id": 3,
                      "gatedName": "item-3",
                      "flakyFromThree": null
                    },
                    {
                      "id": 4,
                      "gatedName": "item-4",
                      "flakyFromThree": null
                    }
                  ]
                }
              ],
              "hasNext": true
            }
            """,
            """
            {
              "incremental": [
                {
                  "id": "2",
                  "errors": [
                    {
                      "message": "flaky item 5",
                      "path": [
                        "gatedItems",
                        4,
                        "flakyFromThree"
                      ]
                    },
                    {
                      "message": "flaky item 6",
                      "path": [
                        "gatedItems",
                        5,
                        "flakyFromThree"
                      ]
                    }
                  ],
                  "items": [
                    {
                      "id": 5,
                      "gatedName": "item-5",
                      "flakyFromThree": null
                    },
                    {
                      "id": 6,
                      "gatedName": "item-6",
                      "flakyFromThree": null
                    }
                  ]
                }
              ],
              "hasNext": true
            }
            """,
            """
            {
              "completed": [
                {
                  "id": "2"
                }
              ],
              "hasNext": false
            }
            """
        ]);
    }

    [Fact]
    public async Task Stream_Should_ReleaseTheBatch_When_BatchIsDisposedAfterRequestWasAborted()
    {
        // arrange
        var state = new StreamState();
        var executor = await CreateExecutorAsync(state);
        using var requestAborted = new CancellationTokenSource();
        var result = await executor.ExecuteAsync(
            "{ gatedItems(count: 3) @stream(initialCount: 1) { id } }",
            requestAborted.Token);
        var stream = (ResponseStream)result;
        state.ReleaseNextItem();

        await using var enumerator = stream
            .ReadResultsAsync()
            .GetAsyncEnumerator(TestContext.Current.CancellationToken);
        await MoveNextOrThrowAsync(enumerator);
        await enumerator.Current.DisposeAsync();
        await MoveNextOrThrowAsync(enumerator);

        var batch = enumerator.Current;
        var held = batch.Incremental.OfType<IncrementalListResult>().Single().Items.Single();
        var element = (ResultElement)held.Element;
        var propertyCountWhileHeld = element.GetPropertyCount();

        // act
        await requestAborted.CancelAsync();
        await state.EnumeratorDisposed.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);
        await batch.DisposeAsync();
        await stream.DisposeAsync();

        // assert
        Assert.Equal(1, held.Index);
        Assert.Equal(1, propertyCountWhileHeld);
        Assert.Throws<ObjectDisposedException>(() => element.GetPropertyCount());
    }

    [Fact]
    public async Task Stream_Should_CompleteWithErrors_When_ItemErrorBubblesAboveTheList()
    {
        // arrange
        var executor = await CreateExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            "{ strictItems(count: 4) @stream(initialCount: 1) { id strict } }",
            TestContext.Current.CancellationToken);

        // assert
        Assert.IsType<ResponseStream>(result).MatchInlineSnapshot(
            """
            {
              "data": {
                "strictItems": [
                  {
                    "id": 1,
                    "strict": "ok-1"
                  }
                ]
              },
              "pending": [
                {
                  "id": "2",
                  "path": [
                    "strictItems"
                  ]
                }
              ],
              "completed": [
                {
                  "id": "2",
                  "errors": [
                    {
                      "message": "strict item",
                      "path": [
                        "strictItems",
                        2,
                        "strict"
                      ]
                    }
                  ]
                }
              ],
              "hasNext": false
            }

            """);
    }

    [Fact]
    public async Task Stream_Should_CompleteWithErrorAndNoItems_When_ItemErrorBubblesAboveRequiredList()
    {
        // arrange
        var executor = await CreateExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            "{ requiredItems(count: 4) @stream(initialCount: 1) { id strict } }",
            TestContext.Current.CancellationToken);

        // assert
        Assert.IsType<ResponseStream>(result).MatchInlineSnapshot(
            """
            {
              "data": {
                "requiredItems": [
                  {
                    "id": 1,
                    "strict": "ok-1"
                  }
                ]
              },
              "pending": [
                {
                  "id": "2",
                  "path": [
                    "requiredItems"
                  ]
                }
              ],
              "completed": [
                {
                  "id": "2",
                  "errors": [
                    {
                      "message": "strict item",
                      "path": [
                        "requiredItems",
                        2,
                        "strict"
                      ]
                    }
                  ]
                }
              ],
              "hasNext": false
            }

            """);
    }

    [Fact]
    public async Task Stream_Should_KeepEarlierItemsAndDropLaterBatch_When_ItemErrorBubblesInLaterBatch()
    {
        // arrange
        var state = new StreamState();
        var executor = await CreateExecutorAsync(state);

        // act
        var payloads = await ReadPayloadsAsync(
                executor,
                "{ gatedItems @stream(initialCount: 1) { id strict } }",
                state.ReleaseNextItem,
                state.ReleaseNextItem)
            .WaitAsync(s_timeout, TestContext.Current.CancellationToken);

        // assert
        payloads.MatchInlineSnapshots(
        [
            """
            {
              "data": {
                "gatedItems": [
                  {
                    "id": 1,
                    "strict": "ok-1"
                  }
                ]
              },
              "pending": [
                {
                  "id": "2",
                  "path": [
                    "gatedItems"
                  ]
                }
              ],
              "hasNext": true
            }
            """,
            """
            {
              "incremental": [
                {
                  "id": "2",
                  "items": [
                    {
                      "id": 2,
                      "strict": "ok-2"
                    }
                  ]
                }
              ],
              "hasNext": true
            }
            """,
            """
            {
              "completed": [
                {
                  "id": "2",
                  "errors": [
                    {
                      "message": "strict item",
                      "path": [
                        "gatedItems",
                        2,
                        "strict"
                      ]
                    }
                  ]
                }
              ],
              "hasNext": false
            }
            """
        ]);
    }

    [Fact]
    public async Task Stream_Should_NotAnnounceStream_When_ItemErrorBubblesInInitialItemsOfRequiredList()
    {
        // arrange
        var executor = await CreateExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            "{ requiredItems(count: 4) @stream(initialCount: 3) { id strict } }",
            TestContext.Current.CancellationToken);

        // assert
        Assert.IsType<ResponseStream>(result).MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "strict item",
                  "path": [
                    "requiredItems",
                    2,
                    "strict"
                  ]
                }
              ],
              "data": null,
              "hasNext": false
            }

            """);
    }

    [Fact]
    public async Task Stream_Should_SendBatchWithNullItem_When_ItemErrorStaysInsideNullableItem()
    {
        // arrange
        var executor = await CreateExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            "{ optionalItems(count: 4) @stream(initialCount: 1) { id strict } }",
            TestContext.Current.CancellationToken);

        // assert
        Assert.IsType<ResponseStream>(result).MatchInlineSnapshot(
            """
            {
              "data": {
                "optionalItems": [
                  {
                    "id": 1,
                    "strict": "ok-1"
                  }
                ]
              },
              "pending": [
                {
                  "id": "2",
                  "path": [
                    "optionalItems"
                  ]
                }
              ],
              "incremental": [
                {
                  "id": "2",
                  "items": [
                    {
                      "id": 2,
                      "strict": "ok-2"
                    },
                    null,
                    {
                      "id": 4,
                      "strict": "ok-4"
                    }
                  ],
                  "errors": [
                    {
                      "message": "strict item",
                      "path": [
                        "optionalItems",
                        2,
                        "strict"
                      ]
                    }
                  ]
                }
              ],
              "completed": [
                {
                  "id": "2"
                }
              ],
              "hasNext": false
            }

            """);
    }

    [Fact]
    public async Task Stream_Should_NotAnnounceNestedDefer_When_BatchWithItemErrorIsDropped()
    {
        // arrange
        var state = new StreamState();
        state.ReleaseHeld(1);
        var executor = await CreateExecutorAsync(state);

        // act
        var payloads = await ReadPayloadsAsync(
                executor,
                """
                {
                    items(count: 4) @stream(initialCount: 1) {
                        id
                        held
                        strict
                        ... @defer {
                            gatedName
                        }
                    }
                }
                """,
                () =>
                {
                    state.ReleaseHeld(2);
                    state.ReleaseHeld(3);
                    state.ReleaseHeld(4);
                    state.ReleaseName(2);
                    state.ReleaseName(3);
                    state.ReleaseName(4);
                },
                () => state.ReleaseName(1))
            .WaitAsync(s_timeout, TestContext.Current.CancellationToken);

        // assert
        payloads.MatchInlineSnapshots(
        [
            """
            {
              "data": {
                "items": [
                  {
                    "id": 1,
                    "held": 1,
                    "strict": "ok-1"
                  }
                ]
              },
              "pending": [
                {
                  "id": "2",
                  "path": [
                    "items",
                    0
                  ]
                },
                {
                  "id": "3",
                  "path": [
                    "items"
                  ]
                }
              ],
              "hasNext": true
            }
            """,
            """
            {
              "completed": [
                {
                  "id": "3",
                  "errors": [
                    {
                      "message": "strict item",
                      "path": [
                        "items",
                        2,
                        "strict"
                      ]
                    }
                  ]
                }
              ],
              "hasNext": true
            }
            """,
            """
            {
              "incremental": [
                {
                  "id": "2",
                  "data": {
                    "gatedName": "item-1"
                  }
                }
              ],
              "completed": [
                {
                  "id": "2"
                }
              ],
              "hasNext": false
            }
            """
        ]);
    }

    [Fact]
    public async Task Stream_Should_CompleteWithErrors_When_EnumeratorThrows()
    {
        // arrange
        var executor = await CreateExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            "{ failingItems @stream(initialCount: 1) { id } }",
            TestContext.Current.CancellationToken);

        // assert
        Assert.IsType<ResponseStream>(result).MatchInlineSnapshot(
            """
            {
              "data": {
                "failingItems": [
                  {
                    "id": 1
                  }
                ]
              },
              "pending": [
                {
                  "id": "2",
                  "path": [
                    "failingItems"
                  ]
                }
              ],
              "incremental": [
                {
                  "id": "2",
                  "items": [
                    {
                      "id": 2
                    }
                  ]
                }
              ],
              "completed": [
                {
                  "id": "2",
                  "errors": [
                    {
                      "message": "enumerator failed",
                      "path": [
                        "failingItems"
                      ]
                    }
                  ]
                }
              ],
              "hasNext": false
            }

            """);
    }

    [Fact]
    public async Task Stream_Should_CompleteWithoutItems_When_InitialCountEqualsListSize()
    {
        // arrange
        var executor = await CreateExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            "{ items(count: 2) @stream(initialCount: 2) { id } }",
            TestContext.Current.CancellationToken);

        // assert
        Assert.IsType<ResponseStream>(result).MatchInlineSnapshot(
            """
            {
              "data": {
                "items": [
                  {
                    "id": 1
                  },
                  {
                    "id": 2
                  }
                ]
              },
              "pending": [
                {
                  "id": "2",
                  "path": [
                    "items"
                  ]
                }
              ],
              "completed": [
                {
                  "id": "2"
                }
              ],
              "hasNext": false
            }

            """);
    }

    [Fact]
    public async Task Stream_Should_NotStream_When_ListEndsWithinInitialCount()
    {
        // arrange
        var executor = await CreateExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            "{ items(count: 2) @stream(initialCount: 3) { id } }",
            TestContext.Current.CancellationToken);

        // assert
        Assert.IsType<OperationResult>(result).MatchInlineSnapshot(
            """
            {
              "data": {
                "items": [
                  {
                    "id": 1
                  },
                  {
                    "id": 2
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task Stream_Should_StreamWithLabel_When_InitialCountAndIfAreVariables()
    {
        // arrange
        var executor = await CreateExecutorAsync();
        var request = OperationRequestBuilder
            .New()
            .SetDocument(
                """
                query ($initialCount: Int!, $stream: Boolean!) {
                    items(count: 3) @stream(label: "fromQuery", initialCount: $initialCount, if: $stream) {
                        id
                    }
                }
                """)
            .SetVariableValues(
                new Dictionary<string, object?>
                {
                    ["initialCount"] = 1,
                    ["stream"] = true
                })
            .Build();

        // act
        var result = await executor.ExecuteAsync(request, TestContext.Current.CancellationToken);

        // assert
        Assert.IsType<ResponseStream>(result).MatchInlineSnapshot(
            """
            {
              "data": {
                "items": [
                  {
                    "id": 1
                  }
                ]
              },
              "pending": [
                {
                  "id": "2",
                  "path": [
                    "items"
                  ],
                  "label": "fromQuery"
                }
              ],
              "incremental": [
                {
                  "id": "2",
                  "items": [
                    {
                      "id": 2
                    },
                    {
                      "id": 3
                    }
                  ]
                }
              ],
              "completed": [
                {
                  "id": "2"
                }
              ],
              "hasNext": false
            }

            """);
    }

    [Fact]
    public async Task Stream_Should_RaiseFieldError_When_InitialCountIsNegative()
    {
        // arrange
        var executor = await CreateExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            "{ items(count: 2) @stream(initialCount: -1) { id } }",
            TestContext.Current.CancellationToken);

        // assert
        Assert.IsType<OperationResult>(result).MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "The `initialCount` argument of the `@stream` directive must not be negative.",
                  "path": [
                    "items"
                  ],
                  "extensions": {
                    "code": "EXEC_INVALID_STREAM_INITIAL_COUNT"
                  }
                }
              ],
              "data": null
            }
            """);
    }

    [Fact]
    public async Task Stream_Should_RaiseFieldError_When_EnumeratorThrowsWhileReadingInitialItems()
    {
        // arrange
        var executor = await CreateExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            "{ failingItems @stream(initialCount: 3) { id } }",
            TestContext.Current.CancellationToken);

        // assert
        Assert.IsType<OperationResult>(result).MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "enumerator failed",
                  "path": [
                    "failingItems"
                  ]
                }
              ],
              "data": null
            }
            """);
    }

    [Fact]
    public async Task Stream_Should_DisposeEnumeratorOnce_When_ResponseStreamIsDisposedMidStream()
    {
        // arrange
        var state = new StreamState();
        var listener = new StreamTaskListener(disposeBeforeStart: false);
        var executor = await CreateBuilder(state)
            .AddDiagnosticEventListener(_ => listener)
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);
        var result = await executor.ExecuteAsync(
            "{ endlessItems @stream(initialCount: 1) { id } }",
            TestContext.Current.CancellationToken);
        var stream = Assert.IsType<ResponseStream>(result);

        // act
        await using (var enumerator = stream
            .ReadResultsAsync()
            .GetAsyncEnumerator(TestContext.Current.CancellationToken))
        {
            Assert.True(await enumerator.MoveNextAsync());
            await enumerator.Current.DisposeAsync();
        }

        await stream.DisposeAsync();
        await state.EnumeratorDisposed.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);
        await listener.TaskCompleted.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(1, state.EnumeratorDisposeCalls);
    }

    [Fact]
    public async Task Stream_Should_DisposeEnumeratorOnce_When_RequestIsAborted()
    {
        // arrange
        var state = new StreamState();
        var listener = new StreamTaskListener(disposeBeforeStart: false);
        var executor = await CreateBuilder(state)
            .AddDiagnosticEventListener(_ => listener)
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);
        using var requestAborted = new CancellationTokenSource();
        var result = await executor.ExecuteAsync(
            "{ endlessItems @stream(initialCount: 1) { id } }",
            requestAborted.Token);
        var stream = Assert.IsType<ResponseStream>(result);

        // act
        await requestAborted.CancelAsync();
        await state.EnumeratorDisposed.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);
        await listener.TaskCompleted.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);
        await stream.DisposeAsync();

        // assert
        Assert.Equal(1, state.EnumeratorDisposeCalls);
    }

    [Fact]
    public async Task Stream_Should_DisposeEnumeratorOnce_When_StreamIsDisposedBeforeStreamTaskStarts()
    {
        // arrange
        var state = new StreamState();
        var listener = new StreamTaskListener(disposeBeforeStart: true);
        var executor = await CreateBuilder(state)
            .AddDiagnosticEventListener(_ => listener)
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor
            .ExecuteAsync(
                "{ endlessItems @stream(initialCount: 1) { id } }",
                TestContext.Current.CancellationToken)
            .WaitAsync(s_timeout, TestContext.Current.CancellationToken);
        var stream = Assert.IsType<ResponseStream>(result);
        await listener.TaskCompleted.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);
        await listener.Disposal.WaitAsync(s_timeout, TestContext.Current.CancellationToken);
        await state.EnumeratorDisposed.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);
        await stream.DisposeAsync();

        // assert
        Assert.Equal(1, state.EnumeratorDisposeCalls);
        Assert.Equal(0, listener.TaskErrorCount);
    }

    [Fact]
    public async Task Stream_Should_EndWithErrors_When_ItemsBatchCannotBeStarted()
    {
        // arrange
        var state = new StreamState();
        var executor = await CreateFaultingExecutorAsync(state, failOnCall: 2);

        // act
        var payloads = await ReadPayloadsAsync(
                executor,
                "{ gatedItems @stream(initialCount: 1) { id } }",
                state.ReleaseNextItem)
            .WaitAsync(s_timeout, TestContext.Current.CancellationToken);

        // assert
        payloads.MatchInlineSnapshots(
        [
            """
            {
              "data": {
                "gatedItems": [
                  {
                    "id": 1
                  }
                ]
              },
              "pending": [
                {
                  "id": "2",
                  "path": [
                    "gatedItems"
                  ]
                }
              ],
              "hasNext": true
            }
            """,
            """
            {
              "completed": [
                {
                  "id": "2",
                  "errors": [
                    {
                      "message": "Unexpected Execution Error",
                      "path": [
                        "gatedItems"
                      ]
                    }
                  ]
                }
              ],
              "hasNext": false
            }
            """
        ]);
    }

    [Fact]
    public async Task Stream_Should_CompleteWithErrors_When_DeferContextCannotBeCreatedForLaterItem()
    {
        // arrange
        var state = new StreamState();
        var executor = await CreateFaultingExecutorAsync(state, failOnCall: 4);

        // act
        var payloads = await ReadPayloadsAsync(
                executor,
                "{ gatedRequiredItems(count: 2) @stream(initialCount: 1) { id ... @defer { gatedName } } }",
                state.ReleaseNextItem,
                () => state.ReleaseName(1))
            .WaitAsync(s_timeout, TestContext.Current.CancellationToken);

        // assert
        payloads.MatchInlineSnapshots(
        [
            """
            {
              "data": {
                "gatedRequiredItems": [
                  {
                    "id": 1
                  }
                ]
              },
              "pending": [
                {
                  "id": "2",
                  "path": [
                    "gatedRequiredItems",
                    0
                  ]
                },
                {
                  "id": "3",
                  "path": [
                    "gatedRequiredItems"
                  ]
                }
              ],
              "hasNext": true
            }
            """,
            """
            {
              "completed": [
                {
                  "id": "3",
                  "errors": [
                    {
                      "message": "Unexpected Execution Error",
                      "path": [
                        "gatedRequiredItems"
                      ]
                    }
                  ]
                }
              ],
              "hasNext": true
            }
            """,
            """
            {
              "incremental": [
                {
                  "id": "2",
                  "data": {
                    "gatedName": "item-1"
                  }
                }
              ],
              "completed": [
                {
                  "id": "2"
                }
              ],
              "hasNext": false
            }
            """
        ]);
    }

    [Theory]
    [InlineData("taskItems")]
    [InlineData("valueTaskItems")]
    [InlineData("queryableItems")]
    [InlineData("executableItems")]
    [InlineData("serviceItems")]
    [InlineData("pureItems")]
    [InlineData("lazyItems")]
    public async Task Stream_Should_BeIgnored_When_ResultIsNotAsyncEnumerable(string field)
    {
        // arrange
        var executor = await CreateExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            $"{{ items: {field}(count: 3) @stream(initialCount: 1) {{ id }} }}",
            TestContext.Current.CancellationToken);

        // assert
        Assert.IsType<OperationResult>(result).MatchInlineSnapshot(
            """
            {
              "data": {
                "items": [
                  {
                    "id": 1
                  },
                  {
                    "id": 2
                  },
                  {
                    "id": 3
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task Stream_Should_StreamValueTypeElements_When_SourceIsAsyncEnumerableOfInt()
    {
        // arrange
        var executor = await CreateExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            "{ numbers(count: 4) @stream(initialCount: 1) }",
            TestContext.Current.CancellationToken);

        // assert
        Assert.IsType<ResponseStream>(result).MatchInlineSnapshot(
            """
            {
              "data": {
                "numbers": [
                  1
                ]
              },
              "pending": [
                {
                  "id": "2",
                  "path": [
                    "numbers"
                  ]
                }
              ],
              "incremental": [
                {
                  "id": "2",
                  "items": [
                    2,
                    3,
                    4
                  ]
                }
              ],
              "completed": [
                {
                  "id": "2"
                }
              ],
              "hasNext": false
            }

            """);
    }

    [Fact]
    public async Task Stream_Should_LimitBatchesToDefaultMaximum_When_MoreItemsAreBufferedThanMaximum()
    {
        // arrange
        var state = new StreamState();
        state.ReleaseHeld(1);
        var executor = await CreateExecutorAsync(state);

        // act
        var payloads = await ReadPayloadsAsync(
            executor,
            "{ items(count: 20) @stream(initialCount: 1) { id held } }",
            () =>
            {
                for (var id = 2; id <= 20; id++)
                {
                    state.ReleaseHeld(id);
                }
            });

        // assert
        payloads.MatchInlineSnapshots(
        [
            """
            {
              "data": {
                "items": [
                  {
                    "id": 1,
                    "held": 1
                  }
                ]
              },
              "pending": [
                {
                  "id": "2",
                  "path": [
                    "items"
                  ]
                }
              ],
              "hasNext": true
            }
            """,
            """
            {
              "incremental": [
                {
                  "id": "2",
                  "items": [
                    {
                      "id": 2,
                      "held": 2
                    },
                    {
                      "id": 3,
                      "held": 3
                    },
                    {
                      "id": 4,
                      "held": 4
                    },
                    {
                      "id": 5,
                      "held": 5
                    },
                    {
                      "id": 6,
                      "held": 6
                    },
                    {
                      "id": 7,
                      "held": 7
                    },
                    {
                      "id": 8,
                      "held": 8
                    },
                    {
                      "id": 9,
                      "held": 9
                    },
                    {
                      "id": 10,
                      "held": 10
                    },
                    {
                      "id": 11,
                      "held": 11
                    },
                    {
                      "id": 12,
                      "held": 12
                    },
                    {
                      "id": 13,
                      "held": 13
                    },
                    {
                      "id": 14,
                      "held": 14
                    },
                    {
                      "id": 15,
                      "held": 15
                    },
                    {
                      "id": 16,
                      "held": 16
                    },
                    {
                      "id": 17,
                      "held": 17
                    }
                  ]
                }
              ],
              "hasNext": true
            }
            """,
            """
            {
              "incremental": [
                {
                  "id": "2",
                  "items": [
                    {
                      "id": 18,
                      "held": 18
                    },
                    {
                      "id": 19,
                      "held": 19
                    },
                    {
                      "id": 20,
                      "held": 20
                    }
                  ]
                }
              ],
              "completed": [
                {
                  "id": "2"
                }
              ],
              "hasNext": false
            }
            """
        ]);
    }

    [Fact]
    public async Task Stream_Should_LimitBatchesToConfiguredMaximum_When_OptionIsChanged()
    {
        // arrange
        var state = new StreamState();
        state.ReleaseHeld(1);
        var executor = await CreateExecutorAsync(state, maxStreamBatchSize: 2);

        // act
        var payloads = await ReadPayloadsAsync(
            executor,
            "{ items(count: 6) @stream(initialCount: 1) { id held } }",
            () =>
            {
                for (var id = 2; id <= 6; id++)
                {
                    state.ReleaseHeld(id);
                }
            });

        // assert
        payloads.MatchInlineSnapshots(
        [
            """
            {
              "data": {
                "items": [
                  {
                    "id": 1,
                    "held": 1
                  }
                ]
              },
              "pending": [
                {
                  "id": "2",
                  "path": [
                    "items"
                  ]
                }
              ],
              "hasNext": true
            }
            """,
            """
            {
              "incremental": [
                {
                  "id": "2",
                  "items": [
                    {
                      "id": 2,
                      "held": 2
                    },
                    {
                      "id": 3,
                      "held": 3
                    }
                  ]
                }
              ],
              "hasNext": true
            }
            """,
            """
            {
              "incremental": [
                {
                  "id": "2",
                  "items": [
                    {
                      "id": 4,
                      "held": 4
                    },
                    {
                      "id": 5,
                      "held": 5
                    }
                  ]
                }
              ],
              "hasNext": true
            }
            """,
            """
            {
              "incremental": [
                {
                  "id": "2",
                  "items": [
                    {
                      "id": 6,
                      "held": 6
                    }
                  ]
                }
              ],
              "completed": [
                {
                  "id": "2"
                }
              ],
              "hasNext": false
            }
            """
        ]);
    }

    [Fact]
    public async Task Stream_Should_DeliverBufferedItemsBeforeErrors_When_SourceThrowsAfterItems()
    {
        // arrange
        var state = new StreamState();
        state.ReleaseHeld(1);
        var executor = await CreateExecutorAsync(state, maxStreamBatchSize: 2);

        // act
        var payloads = await ReadPayloadsAsync(
            executor,
            "{ failingItems(count: 5) @stream(initialCount: 1) { id held } }",
            () =>
            {
                for (var id = 2; id <= 5; id++)
                {
                    state.ReleaseHeld(id);
                }
            });

        // assert
        payloads.MatchInlineSnapshots(
        [
            """
            {
              "data": {
                "failingItems": [
                  {
                    "id": 1,
                    "held": 1
                  }
                ]
              },
              "pending": [
                {
                  "id": "2",
                  "path": [
                    "failingItems"
                  ]
                }
              ],
              "hasNext": true
            }
            """,
            """
            {
              "incremental": [
                {
                  "id": "2",
                  "items": [
                    {
                      "id": 2,
                      "held": 2
                    },
                    {
                      "id": 3,
                      "held": 3
                    }
                  ]
                }
              ],
              "hasNext": true
            }
            """,
            """
            {
              "incremental": [
                {
                  "id": "2",
                  "items": [
                    {
                      "id": 4,
                      "held": 4
                    },
                    {
                      "id": 5,
                      "held": 5
                    }
                  ]
                }
              ],
              "hasNext": true
            }
            """,
            """
            {
              "completed": [
                {
                  "id": "2",
                  "errors": [
                    {
                      "message": "enumerator failed",
                      "path": [
                        "failingItems"
                      ]
                    }
                  ]
                }
              ],
              "hasNext": false
            }
            """
        ]);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void MaxStreamBatchSize_Should_BeClampedToOne_When_ValueIsBelowOne(int value)
    {
        // arrange
        var options = new RequestExecutorOptions();

        // act
        options.MaxStreamBatchSize = value;

        // assert
        Assert.Equal(1, options.MaxStreamBatchSize);
    }

    [Fact]
    public void MaxStreamBatchSize_Should_DefaultToSixteen_When_NotConfigured()
    {
        // arrange
        // act
        var options = new RequestExecutorOptions();

        // assert
        Assert.Equal(16, options.MaxStreamBatchSize);
    }

    [Fact]
    public async Task Stream_Should_StopProducerAndDisposeOnce_When_ClientDisconnectsWhileBufferIsFull()
    {
        // arrange
        var state = new StreamState();
        var listener = new StreamTaskListener(disposeBeforeStart: false);
        var executor = await CreateBuilder(state)
            .AddDiagnosticEventListener(_ => listener)
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);
        using var requestAborted = new CancellationTokenSource();
        var result = await executor.ExecuteAsync(
            "{ bufferingItems @stream(initialCount: 1) { id blockedOnSecond } }",
            requestAborted.Token);
        var stream = Assert.IsType<ResponseStream>(result);
        await state.SecondItemBlocked.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);

        // act
        state.ReleaseNextItem();
        await WaitUntilAsync(() => state.Produced >= 67);
        await requestAborted.CancelAsync();
        await state.EnumeratorDisposed.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);
        await listener.TaskCompleted.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);
        await stream.DisposeAsync();

        // assert
        Assert.Equal(1, state.EnumeratorDisposeCalls);
        Assert.Equal(67, state.Produced);
    }

    [Fact]
    public async Task Stream_Should_StopSourceAndDisposeOnce_When_FailedBatchLeavesMoreItems()
    {
        // arrange
        var state = new StreamState();
        var executor = await CreateExecutorAsync(state);

        // act
        var payloads = await ReadPayloadsAsync(
                executor,
                "{ gatedItems(count: 6) @stream(initialCount: 1) { id strict } }",
                state.ReleaseNextItem,
                state.ReleaseNextItem)
            .WaitAsync(s_timeout, TestContext.Current.CancellationToken);
        await state.EnumeratorDisposed.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(1, state.EnumeratorDisposeCalls);
        payloads.MatchInlineSnapshots(
        [
            """
            {
              "data": {
                "gatedItems": [
                  {
                    "id": 1,
                    "strict": "ok-1"
                  }
                ]
              },
              "pending": [
                {
                  "id": "2",
                  "path": [
                    "gatedItems"
                  ]
                }
              ],
              "hasNext": true
            }
            """,
            """
            {
              "incremental": [
                {
                  "id": "2",
                  "items": [
                    {
                      "id": 2,
                      "strict": "ok-2"
                    }
                  ]
                }
              ],
              "hasNext": true
            }
            """,
            """
            {
              "completed": [
                {
                  "id": "2",
                  "errors": [
                    {
                      "message": "strict item",
                      "path": [
                        "gatedItems",
                        2,
                        "strict"
                      ]
                    }
                  ]
                }
              ],
              "hasNext": false
            }
            """
        ]);
    }

    [Fact]
    public async Task Stream_Should_EndResponse_When_DroppedBatchHasNestedStreamWithNestedDefer()
    {
        // arrange
        var state = new StreamState();
        state.ReleaseHeld(1);
        state.ReleaseStrict(1);
        var executor = await CreateExecutorAsync(state);

        // act
        var payloads = await ReadPayloadsWithStepsAsync(
                executor,
                """
                {
                    items(count: 3) @stream(initialCount: 1) {
                        id
                        held
                        gatedStrict
                        nestedChildren @stream(initialCount: 0) {
                            id
                            ... @defer {
                                gatedName
                            }
                        }
                    }
                }
                """,
                [
                    async () =>
                    {
                        state.ReleaseHeld(2);
                        state.ReleaseHeld(3);
                        await state.WaitForNameStartedAsync(21);
                        await state.WaitForNameStartedAsync(31);
                        state.ReleaseStrict(2);
                        state.ReleaseStrict(3);
                        state.ReleaseName(21);
                        state.ReleaseName(31);
                    }
                ])
            .WaitAsync(s_timeout, TestContext.Current.CancellationToken);
        await state.NestedDisposed.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(2, state.NestedDisposeCalls);
        payloads.MatchInlineSnapshots(
        [
            """
            {
              "data": {
                "items": [
                  {
                    "id": 1,
                    "held": 1,
                    "gatedStrict": "ok-1",
                    "nestedChildren": null
                  }
                ]
              },
              "pending": [
                {
                  "id": "2",
                  "path": [
                    "items"
                  ]
                }
              ],
              "hasNext": true
            }
            """,
            """
            {
              "completed": [
                {
                  "id": "2",
                  "errors": [
                    {
                      "message": "gated strict item",
                      "path": [
                        "items",
                        2,
                        "gatedStrict"
                      ]
                    }
                  ]
                }
              ],
              "hasNext": false
            }
            """
        ]);
    }

    [Fact]
    public async Task Stream_Should_EndResponse_When_DroppedBatchHasNestedStreamThatNeverEnds()
    {
        // arrange
        var state = new StreamState();
        state.ReleaseHeld(1);
        state.ReleaseStrict(1);
        var executor = await CreateExecutorAsync(state);

        // act
        var payloads = await ReadPayloadsWithStepsAsync(
                executor,
                """
                {
                    items(count: 3) @stream(initialCount: 1) {
                        id
                        held
                        gatedStrict
                        nestedChildren @stream(initialCount: 0) {
                            id
                            gatedName
                        }
                    }
                }
                """,
                [
                    async () =>
                    {
                        state.ReleaseHeld(2);
                        state.ReleaseHeld(3);
                        await state.WaitForNameStartedAsync(21);
                        await state.WaitForNameStartedAsync(31);
                        state.ReleaseStrict(2);
                        state.ReleaseStrict(3);
                        state.ReleaseName(21);
                        state.ReleaseName(31);
                    }
                ])
            .WaitAsync(s_timeout, TestContext.Current.CancellationToken);
        await state.NestedDisposed.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(2, state.NestedDisposeCalls);
        payloads.MatchInlineSnapshots(
        [
            """
            {
              "data": {
                "items": [
                  {
                    "id": 1,
                    "held": 1,
                    "gatedStrict": "ok-1",
                    "nestedChildren": null
                  }
                ]
              },
              "pending": [
                {
                  "id": "2",
                  "path": [
                    "items"
                  ]
                }
              ],
              "hasNext": true
            }
            """,
            """
            {
              "completed": [
                {
                  "id": "2",
                  "errors": [
                    {
                      "message": "gated strict item",
                      "path": [
                        "items",
                        2,
                        "gatedStrict"
                      ]
                    }
                  ]
                }
              ],
              "hasNext": false
            }
            """
        ]);
    }

    [Fact]
    public async Task Stream_Should_EndResponse_When_DroppedBatchHasNestedStreamThatIgnoresCancellation()
    {
        // arrange
        var state = new StreamState();
        state.ReleaseHeld(1);
        state.ReleaseStrict(1);
        var executor = await CreateExecutorAsync(state);

        // act
        var payloads = await ReadPayloadsWithStepsAsync(
                executor,
                """
                {
                    items(count: 3) @stream(initialCount: 1) {
                        id
                        held
                        gatedStrict
                        stuckChildren @stream(initialCount: 0) {
                            id
                            gatedName
                        }
                    }
                }
                """,
                [
                    async () =>
                    {
                        state.ReleaseHeld(2);
                        state.ReleaseHeld(3);
                        await state.WaitForNameStartedAsync(21);
                        await state.WaitForNameStartedAsync(31);
                        state.ReleaseStrict(2);
                        state.ReleaseStrict(3);
                        state.ReleaseName(21);
                        state.ReleaseName(31);
                    }
                ])
            .WaitAsync(s_timeout, TestContext.Current.CancellationToken);
        state.ReleaseChildren(2);
        state.ReleaseChildren(3);
        await WaitUntilAsync(() => state.EnumeratorDisposeCalls >= 2);

        // assert
        Assert.Equal(2, state.EnumeratorDisposeCalls);
        payloads.MatchInlineSnapshots(
        [
            """
            {
              "data": {
                "items": [
                  {
                    "id": 1,
                    "held": 1,
                    "gatedStrict": "ok-1",
                    "stuckChildren": null
                  }
                ]
              },
              "pending": [
                {
                  "id": "2",
                  "path": [
                    "items"
                  ]
                }
              ],
              "hasNext": true
            }
            """,
            """
            {
              "completed": [
                {
                  "id": "2",
                  "errors": [
                    {
                      "message": "gated strict item",
                      "path": [
                        "items",
                        2,
                        "gatedStrict"
                      ]
                    }
                  ]
                }
              ],
              "hasNext": false
            }
            """
        ]);
    }

    [Fact]
    public async Task Stream_Should_CompleteResponse_When_FailedBatchSourceIgnoresCancellation()
    {
        // arrange
        var state = new StreamState();
        var executor = await CreateExecutorAsync(state);

        // act
        var payloads = await ReadPayloadsAsync(
                executor,
                "{ stuckItems(count: 6) @stream(initialCount: 1) { id strict } }",
                state.ReleaseNextItem,
                state.ReleaseNextItem)
            .WaitAsync(s_timeout, TestContext.Current.CancellationToken);
        state.ReleaseNextItem();
        await state.EnumeratorDisposed.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(1, state.EnumeratorDisposeCalls);
        payloads.MatchInlineSnapshots(
        [
            """
            {
              "data": {
                "stuckItems": [
                  {
                    "id": 1,
                    "strict": "ok-1"
                  }
                ]
              },
              "pending": [
                {
                  "id": "2",
                  "path": [
                    "stuckItems"
                  ]
                }
              ],
              "hasNext": true
            }
            """,
            """
            {
              "incremental": [
                {
                  "id": "2",
                  "items": [
                    {
                      "id": 2,
                      "strict": "ok-2"
                    }
                  ]
                }
              ],
              "hasNext": true
            }
            """,
            """
            {
              "completed": [
                {
                  "id": "2",
                  "errors": [
                    {
                      "message": "strict item",
                      "path": [
                        "stuckItems",
                        2,
                        "strict"
                      ]
                    }
                  ]
                }
              ],
              "hasNext": false
            }
            """
        ]);
    }

    [Fact]
    public async Task Stream_Should_PutAllItemsInInitialPayload_When_InitialCountExceedsBatchMaximum()
    {
        // arrange
        var state = new StreamState();
        var executor = await CreateExecutorAsync(state, maxStreamBatchSize: 16);

        // act
        var result = await executor.ExecuteAsync(
            "{ numbers(count: 41) @stream(initialCount: 40) }",
            TestContext.Current.CancellationToken);

        // assert
        Assert.IsType<ResponseStream>(result).MatchInlineSnapshot(
            """
            {
              "data": {
                "numbers": [
                  1,
                  2,
                  3,
                  4,
                  5,
                  6,
                  7,
                  8,
                  9,
                  10,
                  11,
                  12,
                  13,
                  14,
                  15,
                  16,
                  17,
                  18,
                  19,
                  20,
                  21,
                  22,
                  23,
                  24,
                  25,
                  26,
                  27,
                  28,
                  29,
                  30,
                  31,
                  32,
                  33,
                  34,
                  35,
                  36,
                  37,
                  38,
                  39,
                  40
                ]
              },
              "pending": [
                {
                  "id": "2",
                  "path": [
                    "numbers"
                  ]
                }
              ],
              "incremental": [
                {
                  "id": "2",
                  "items": [
                    41
                  ]
                }
              ],
              "completed": [
                {
                  "id": "2"
                }
              ],
              "hasNext": false
            }

            """);
    }

    [Fact]
    public async Task Stream_Should_DeliverInitialCountItems_When_SourceWaitsBeforeEveryItem()
    {
        // arrange
        var state = new StreamState();
        var executor = await CreateExecutorAsync(state);

        // act
        var execution = executor.ExecuteAsync(
            "{ gatedItems(count: 5) @stream(initialCount: 5) { id } }",
            TestContext.Current.CancellationToken);
        state.ReleaseNextItem();
        state.ReleaseNextItem();
        state.ReleaseNextItem();
        await Assert.ThrowsAsync<TimeoutException>(
            () => execution.WaitAsync(TimeSpan.FromMilliseconds(250), TestContext.Current.CancellationToken));
        state.ReleaseNextItem();
        var result = await execution.WaitAsync(s_timeout, TestContext.Current.CancellationToken);
        var payloads = new List<string>();

        await foreach (var payload in Assert.IsType<ResponseStream>(result)
            .ReadResultsAsync()
            .WithCancellation(TestContext.Current.CancellationToken))
        {
            await using var cleanup = payload;
            payloads.Add(payload.ToJson());
            state.ReleaseNextItem();
        }

        // assert
        payloads.MatchInlineSnapshots(
        [
            """
            {
              "data": {
                "gatedItems": [
                  {
                    "id": 1
                  },
                  {
                    "id": 2
                  },
                  {
                    "id": 3
                  },
                  {
                    "id": 4
                  },
                  {
                    "id": 5
                  }
                ]
              },
              "pending": [
                {
                  "id": "2",
                  "path": [
                    "gatedItems"
                  ]
                }
              ],
              "hasNext": true
            }
            """,
            """
            {
              "completed": [
                {
                  "id": "2"
                }
              ],
              "hasNext": false
            }
            """
        ]);
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeout.CancelAfter(s_timeout);

        while (!condition())
        {
            await Task.Delay(5, timeout.Token);
        }
    }

    private static async Task<IRequestExecutor> CreateExecutorAsync(StreamState state, int maxStreamBatchSize)
        => await CreateBuilder(state)
            .ModifyRequestOptions(o => o.MaxStreamBatchSize = maxStreamBatchSize)
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

    private static async Task<IRequestExecutor> CreateExecutorAsync(StreamState? state = null)
        => await CreateBuilder(state ?? new StreamState()).BuildRequestExecutorAsync();

    private static async Task<IRequestExecutor> CreateFaultingExecutorAsync(StreamState state, int failOnCall)
    {
        var builder = CreateBuilder(state);
        builder.Services.AddSingleton<IFactory<OperationContextOwner>>(
            sp => new FaultingContextOwnerFactory(
                new OperationContextOwnerFactory(sp.GetRequiredService<ObjectPool<OperationContext>>()),
                failOnCall));

        return await builder.BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);
    }

    private static IRequestExecutorBuilder CreateBuilder(StreamState state)
        => new ServiceCollection()
            .AddSingleton(state)
            .AddGraphQL()
            .AddQueryType<StreamQuery>()
            .AddType<ConnectionLikeType>()
            .AddDataLoader<StreamLoadedDataLoader>()
            .ModifyOptions(
                o =>
                {
                    o.EnableDefer = true;
                    o.EnableStream = true;
                });

    private static async Task MoveNextOrThrowAsync(IAsyncEnumerator<OperationResult> enumerator)
    {
        if (!await enumerator.MoveNextAsync())
        {
            throw new InvalidOperationException("The response stream ended before the expected payload.");
        }
    }

    private static async Task<List<string>> ReadErrorPathsAsync(ResponseStream stream)
    {
        var paths = new List<string>();

        await foreach (var payload in stream
            .ReadResultsAsync()
            .WithCancellation(TestContext.Current.CancellationToken))
        {
            await using var cleanup = payload;
            paths.AddRange(GetErrorPaths(payload.ToJson()));
        }

        return paths;
    }

    private static IEnumerable<string> GetErrorPaths(string payload)
    {
        using var document = JsonDocument.Parse(payload);
        var paths = new List<string>();

        if (document.RootElement.TryGetProperty("incremental", out var incremental))
        {
            foreach (var entry in incremental.EnumerateArray())
            {
                if (entry.TryGetProperty("errors", out var errors))
                {
                    paths.AddRange(
                        errors.EnumerateArray().Select(e => JsonSerializer.Serialize(e.GetProperty("path"))));
                }
            }
        }

        return paths;
    }

    private static List<string> GetPendingPaths(IEnumerable<string> payloads)
    {
        var paths = new List<string>();

        foreach (var payload in payloads)
        {
            using var document = JsonDocument.Parse(payload);

            if (document.RootElement.TryGetProperty("pending", out var pending))
            {
                paths.AddRange(pending.EnumerateArray().Select(e => JsonSerializer.Serialize(e.GetProperty("path"))));
            }
        }

        paths.Sort(StringComparer.Ordinal);
        return paths;
    }

    // Runs one step after each payload that was read, which lets a test hold back the producers of
    // later payloads until the earlier payloads were observed.
    private static async Task<List<string>> ReadPayloadsAsync(
        IRequestExecutor executor,
        string query,
        params Action[] steps)
        => await ReadPayloadsWithStepsAsync(
            executor,
            query,
            [.. steps.Select(step => new Func<Task>(
                () =>
                {
                    step();
                    return Task.CompletedTask;
                }))]);

    private static async Task<List<string>> ReadPayloadsWithStepsAsync(
        IRequestExecutor executor,
        string query,
        Func<Task>[] steps)
    {
        var result = await executor.ExecuteAsync(query, TestContext.Current.CancellationToken);
        var payloads = new List<string>();
        var nextStep = 0;

        await foreach (var payload in Assert.IsType<ResponseStream>(result)
            .ReadResultsAsync()
            .WithCancellation(TestContext.Current.CancellationToken))
        {
            await using var cleanup = payload;
            payloads.Add(payload.ToJson());

            if (nextStep < steps.Length)
            {
                await steps[nextStep++]().WaitAsync(s_timeout, TestContext.Current.CancellationToken);
            }
        }

        return payloads;
    }

    public sealed class StreamState
    {
        private readonly SemaphoreSlim _items = new(0);
        private readonly ConcurrentDictionary<int, TaskCompletionSource> _names = new();
        private readonly ConcurrentDictionary<int, TaskCompletionSource> _children = new();
        private readonly ConcurrentDictionary<int, TaskCompletionSource> _childrenEnd = new();
        private readonly ConcurrentDictionary<int, TaskCompletionSource> _held = new();
        private readonly ConcurrentDictionary<int, TaskCompletionSource> _namesStarted = new();
        private readonly ConcurrentDictionary<int, TaskCompletionSource> _strict = new();
        private readonly List<string> _batches = [];
        private int _enumeratorDisposeCalls;
        private int _nestedDisposeCalls;
        private int _produced;

        public IReadOnlyList<string> Batches
        {
            get
            {
                lock (_batches)
                {
                    return [.. _batches];
                }
            }
        }

        public int EnumeratorDisposeCalls => Volatile.Read(ref _enumeratorDisposeCalls);

        public int NestedDisposeCalls => Volatile.Read(ref _nestedDisposeCalls);

        public int Produced => Volatile.Read(ref _produced);

        public TaskCompletionSource NestedDisposed { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource SecondItemBlocked { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void ReleaseStrict(int id) => GetGate(_strict, id).TrySetResult();

        public Task WaitForStrictAsync(int id) => GetGate(_strict, id).Task;

        public void NameStarted(int id) => GetGate(_namesStarted, id).TrySetResult();

        public Task WaitForNameStartedAsync(int id) => GetGate(_namesStarted, id).Task;

        public void ItemProduced() => Interlocked.Increment(ref _produced);

        public void NestedDisposedCalled()
        {
            if (Interlocked.Increment(ref _nestedDisposeCalls) == 2)
            {
                NestedDisposed.TrySetResult();
            }
        }

        public TaskCompletionSource EnumeratorDisposed { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void ReleaseNextItem() => _items.Release();

        public Task WaitForNextItemAsync(CancellationToken cancellationToken) => _items.WaitAsync(cancellationToken);

        public void ReleaseName(int id) => GetGate(_names, id).TrySetResult();

        public Task WaitForNameAsync(int id) => GetGate(_names, id).Task;

        public void ReleaseChildren(int id) => GetGate(_children, id).TrySetResult();

        public Task WaitForChildrenAsync(int id) => GetGate(_children, id).Task;

        public void ReleaseHeld(int id) => GetGate(_held, id).TrySetResult();

        public Task WaitForHeldAsync(int id) => GetGate(_held, id).Task;

        public void ReleaseChildrenEnd(int id) => GetGate(_childrenEnd, id).TrySetResult();

        public Task WaitForChildrenEndAsync(int id) => GetGate(_childrenEnd, id).Task;

        public void AddBatch(IReadOnlyList<int> keys)
        {
            lock (_batches)
            {
                _batches.Add(string.Join(",", keys.Order()));
            }
        }

        public void EnumeratorDisposedCalled()
        {
            Interlocked.Increment(ref _enumeratorDisposeCalls);
            EnumeratorDisposed.TrySetResult();
        }

        private static TaskCompletionSource GetGate(ConcurrentDictionary<int, TaskCompletionSource> gates, int id)
            => gates.GetOrAdd(id, _ => new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously));
    }

    public sealed class StreamLoadedDataLoader(
        StreamState state,
        IBatchScheduler batchScheduler,
        DataLoaderOptions options)
        : BatchDataLoader<int, string>(batchScheduler, options)
    {
        protected override Task<IReadOnlyDictionary<int, string>> LoadBatchAsync(
            IReadOnlyList<int> keys,
            CancellationToken cancellationToken)
        {
            state.AddBatch(keys);
            return Task.FromResult<IReadOnlyDictionary<int, string>>(keys.ToDictionary(k => k, k => $"loaded-{k}"));
        }
    }

    public sealed class Item(int id)
    {
        public int Id => id;

        public async Task<string> GetGatedNameAsync([Service] StreamState state)
        {
            state.NameStarted(id);
            await state.WaitForNameAsync(id);
            return $"item-{id}";
        }

        public async Task<string> GetGatedStrictAsync([Service] StreamState state)
        {
            await state.WaitForStrictAsync(id);

            return id == 3
                ? throw new GraphQLException("gated strict item")
                : $"ok-{id}";
        }

        public async Task<string> GetBlockedOnSecondAsync(
            [Service] StreamState state,
            CancellationToken cancellationToken)
        {
            if (id == 2)
            {
                state.SecondItemBlocked.TrySetResult();
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }

            return $"ok-{id}";
        }

        public IAsyncEnumerable<Item>? GetNestedChildren([Service] StreamState state)
            => id == 1 ? null : ProduceNestedChildrenAsync(state);

        private async IAsyncEnumerable<Item> ProduceNestedChildrenAsync(
            StreamState state,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            try
            {
                yield return new Item((id * 10) + 1);

                await state.WaitForChildrenAsync(id).WaitAsync(cancellationToken);

                yield return new Item((id * 10) + 2);
            }
            finally
            {
                state.NestedDisposedCalled();
            }
        }

        public IAsyncEnumerable<Item>? GetStuckChildren([Service] StreamState state)
            => id == 1 ? null : ProduceStuckChildrenAsync(state);

        private async IAsyncEnumerable<Item> ProduceStuckChildrenAsync(StreamState state)
        {
            try
            {
                yield return new Item((id * 10) + 1);

                await state.WaitForChildrenAsync(id);

                yield return new Item((id * 10) + 2);
            }
            finally
            {
                state.EnumeratorDisposedCalled();
            }
        }

        public async Task<int> GetHeldAsync([Service] StreamState state)
        {
            await state.WaitForHeldAsync(id);
            return id;
        }

        public async IAsyncEnumerable<Item> GetGatedChildren(
            [Service] StreamState state,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            yield return new Item((id * 10) + 1);

            await state.WaitForChildrenAsync(id).WaitAsync(cancellationToken);

            yield return new Item((id * 10) + 2);

            await state.WaitForChildrenEndAsync(id).WaitAsync(cancellationToken);
        }

        public async Task<string> GetLoadedAsync(StreamLoadedDataLoader loader, CancellationToken cancellationToken)
            => await loader.LoadRequiredAsync(id, cancellationToken);

        public async Task<string> GetLoadedAfterFirstAsync(
            StreamLoadedDataLoader loader,
            CancellationToken cancellationToken)
            => id == 1 ? "initial" : await loader.LoadRequiredAsync(id, cancellationToken);

        public async Task<string?> GetFlakyFromThreeAsync()
        {
            await Task.Yield();

            return id >= 3
                ? throw new GraphQLException($"flaky item {id}")
                : $"ok-{id}";
        }

        public async Task<string?> GetFlakyAsync()
        {
            await Task.Yield();

            return id == 3
                ? throw new GraphQLException("flaky item")
                : $"ok-{id}";
        }

        public async Task<string> GetStrictAsync()
        {
            await Task.Yield();

            return id == 3
                ? throw new GraphQLException("strict item")
                : $"ok-{id}";
        }
    }

    public sealed class StreamQuery
    {
        public IAsyncEnumerable<Item> GetItems(int count)
            => ProduceItemsAsync(count);

        [GraphQLType<NonNullType<ListType<NonNullType<ObjectType<Item>>>>>]
        public IAsyncEnumerable<Item> GetRequiredItems(int count)
            => ProduceItemsAsync(count);

        [GraphQLType<NonNullType<ListType<ObjectType<Item>>>>]
        public IAsyncEnumerable<Item> GetOptionalItems(int count)
            => ProduceItemsAsync(count);

        [GraphQLType<ListType<NonNullType<ObjectType<Item>>>>]
        public IAsyncEnumerable<Item> GetStrictItems(int count)
            => ProduceItemsAsync(count);

        public async IAsyncEnumerable<int> GetNumbers(int count)
        {
            await Task.Yield();

            for (var i = 1; i <= count; i++)
            {
                yield return i;
            }
        }

        public async Task<List<Item>> GetTaskItems(int count)
        {
            await Task.Yield();
            return CreateItems(count);
        }

        public async ValueTask<List<Item>> GetValueTaskItems(int count)
        {
            await Task.Yield();
            return CreateItems(count);
        }

        public IQueryable<Item> GetQueryableItems(int count)
            => CreateItems(count).AsQueryable();

        public IExecutable<Item> GetExecutableItems(int count)
            => Executable.From(CreateItems(count).AsQueryable());

        public List<Item> GetServiceItems([Service] StreamState state, int count)
            => CreateItems(count);

        public List<Item> GetPureItems(int count)
            => CreateItems(count);

        public IEnumerable<Item> GetLazyItems(int count)
        {
            for (var i = 1; i <= count; i++)
            {
                yield return new Item(i);
            }
        }

        public async IAsyncEnumerable<Item> GetBurstItems(
            [Service] StreamState state,
            [EnumeratorCancellation] CancellationToken cancellationToken,
            int count = 2)
        {
            yield return new Item(1);

            await state.WaitForNextItemAsync(cancellationToken);

            for (var i = 2; i <= count; i++)
            {
                yield return new Item(i);
            }

            await state.WaitForNextItemAsync(cancellationToken);
        }

        [GraphQLType<NonNullType<ObjectType<PageConnection<Item>>>>]
        public PageConnection<Item> GetPagedItems()
            => new(Page<Item>.Create(
                [new Item(1), new Item(2)],
                hasNextPage: false,
                hasPreviousPage: false,
                item => item.Id.ToString()));

        public ConnectionLike GetConnectionLike()
            => new();

        public async IAsyncEnumerable<string> GetBurstNames(
            [Service] StreamState state,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            yield return "a";

            await state.WaitForNextItemAsync(cancellationToken);

            yield return "b";

            await state.WaitForNextItemAsync(cancellationToken);
        }

        public IAsyncEnumerable<Item> GetGatedItems(
            [Service] StreamState state,
            int count = 3)
            => ProduceGatedItemsAsync(state, count);

        [GraphQLType<NonNullType<ListType<NonNullType<ObjectType<Item>>>>>]
        public IAsyncEnumerable<Item> GetGatedRequiredItems(
            [Service] StreamState state,
            int count = 3)
            => ProduceGatedItemsAsync(state, count);

        public async IAsyncEnumerable<Item> GetFailingItems(
            [EnumeratorCancellation] CancellationToken cancellationToken,
            int count = 2)
        {
            for (var i = 1; i <= count; i++)
            {
                yield return new Item(i);
            }

            await Task.Delay(10, cancellationToken);
            throw new GraphQLException("enumerator failed");
        }

        public IAsyncEnumerable<Item> GetStuckItems(
            [Service] StreamState state,
            int count = 3)
            => ProduceStuckItemsAsync(state, count);

        public IAsyncEnumerable<Item> GetEndlessItems([Service] StreamState state)
            => new EndlessItems(state);

        public async IAsyncEnumerable<Item> GetBufferingItems(
            [Service] StreamState state,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            try
            {
                state.ItemProduced();
                yield return new Item(1);

                state.ItemProduced();
                yield return new Item(2);

                await state.WaitForNextItemAsync(cancellationToken);

                for (var i = 3; ; i++)
                {
                    state.ItemProduced();
                    yield return new Item(i);
                }
            }
            finally
            {
                state.EnumeratorDisposedCalled();
            }
        }

        private static List<Item> CreateItems(int count)
            => [.. Enumerable.Range(1, count).Select(i => new Item(i))];

        private static async IAsyncEnumerable<Item> ProduceGatedItemsAsync(
            StreamState state,
            int count,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            try
            {
                state.ItemProduced();
                yield return new Item(1);

                for (var i = 2; i <= count; i++)
                {
                    await state.WaitForNextItemAsync(cancellationToken);
                    state.ItemProduced();
                    yield return new Item(i);
                }

                await state.WaitForNextItemAsync(cancellationToken);
            }
            finally
            {
                state.EnumeratorDisposedCalled();
            }
        }

        private static async IAsyncEnumerable<Item> ProduceStuckItemsAsync(StreamState state, int count)
        {
            try
            {
                yield return new Item(1);

                for (var i = 2; i <= count; i++)
                {
                    await state.WaitForNextItemAsync(CancellationToken.None);
                    yield return new Item(i);
                }
            }
            finally
            {
                state.EnumeratorDisposedCalled();
            }
        }

        private static async IAsyncEnumerable<Item> ProduceItemsAsync(int count)
        {
            await Task.Yield();

            for (var i = 1; i <= count; i++)
            {
                yield return new Item(i);
            }
        }
    }

    public sealed class ConnectionLike
    {
        public async IAsyncEnumerable<Item> GetNodes()
        {
            yield return new Item(1);
            await Task.Yield();
            yield return new Item(2);
        }

        public async IAsyncEnumerable<Item> GetEdges()
        {
            yield return new Item(1);
            await Task.Yield();
            yield return new Item(2);
        }
    }

    public sealed class ConnectionLikeType : ObjectType<ConnectionLike>
    {
        protected override void Configure(IObjectTypeDescriptor<ConnectionLike> descriptor)
        {
            descriptor
                .Field(t => t.GetNodes())
                .Extend()
                .OnBeforeCreate((_, field) => field.SetConnectionNodesFieldFlags());

            descriptor
                .Field(t => t.GetEdges())
                .Extend()
                .OnBeforeCreate((_, field) => field.SetConnectionEdgesFieldFlags());
        }
    }

    private sealed class StreamTaskListener(bool disposeBeforeStart) : ExecutionDiagnosticEventListener
    {
        private int _taskErrorCount;

        public int TaskErrorCount => Volatile.Read(ref _taskErrorCount);

        public Task Disposal { get; private set; } = Task.CompletedTask;

        public TaskCompletionSource TaskCompleted { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public override IDisposable RunTask(IExecutionTask task)
        {
            if (!task.IsDeferred || task.BranchId != BranchTracker.SystemBranchId)
            {
                return EmptyScope;
            }

            if (disposeBeforeStart && task is StreamTask streamTask)
            {
                Disposal = streamTask.DisposeAsync().AsTask();
            }

            return new CompletionScope(TaskCompleted);
        }

        public override void TaskError(IExecutionTask task, IError error)
            => Interlocked.Increment(ref _taskErrorCount);

        private sealed class CompletionScope(TaskCompletionSource completed) : IDisposable
        {
            public void Dispose() => completed.TrySetResult();
        }
    }

    private sealed class FaultingContextOwnerFactory(IFactory<OperationContextOwner> inner, int failOnCall)
        : IFactory<OperationContextOwner>
    {
        private int _calls;

        public OperationContextOwner Create()
            => Interlocked.Increment(ref _calls) == failOnCall
                ? throw new InvalidOperationException("context unavailable")
                : inner.Create();
    }

    private sealed class EndlessItems(StreamState state) : IAsyncEnumerable<Item>
    {
        public IAsyncEnumerator<Item> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            => new Enumerator(state, cancellationToken);

        private sealed class Enumerator(StreamState state, CancellationToken cancellationToken)
            : IAsyncEnumerator<Item>
        {
            private int _index;

            public Item Current { get; private set; } = null!;

            public async ValueTask<bool> MoveNextAsync()
            {
                if (_index > 0)
                {
                    await Task.Delay(10, cancellationToken);
                }

                Current = new Item(++_index);
                return true;
            }

            public ValueTask DisposeAsync()
            {
                state.EnumeratorDisposedCalled();
                return ValueTask.CompletedTask;
            }
        }
    }
}
