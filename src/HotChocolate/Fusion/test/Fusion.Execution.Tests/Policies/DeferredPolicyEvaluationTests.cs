using System.Text.Json;
using System.Text.Json.Nodes;
using HotChocolate.Execution;
using HotChocolate.Fusion.Policies.InMemory;

namespace HotChocolate.Fusion.Policies;

public class DeferredPolicyEvaluationTests : PolicyExecutionTestBase
{
    private const string Schema =
        """
        type Query {
          product: Product
          products: [Product]
        }

        type Product {
          id: ID!
          price: Int
          cost: Int @policy(policies: [["finance"]])
        }
        """;

    private const string ProductSchema =
        """
        type Query {
          productById(id: ID!): Product @lookup
        }

        type Mutation {
          updateProduct: Product
        }

        type Product @key(fields: "id") {
          id: ID!
        }
        """;

    private const string ProductDetailsSchema =
        """
        type Query {
          productDetailsById(id: ID!): Product @lookup @internal
        }

        type Product @key(fields: "id") {
          id: ID!
          price: Int
          stock: Int
          cost: Int @policy(policies: [["finance"]])
        }
        """;

    private const string Data =
        """
        {
          "product": { "id": "1", "price": 10, "cost": 5 },
          "products": [
            { "id": "1", "price": 10, "cost": 5 },
            { "id": "2", "price": 20, "cost": 8 }
          ],
          "updateProduct": { "id": "1", "price": 10, "cost": 5 },
          "productById": { "id": "1" },
          "productDetailsById": { "id": "1", "price": 10, "stock": 7, "cost": 5 }
        }
        """;

    [Theory]
    [InlineData(true, "5")]
    [InlineData(false, "null")]
    public async Task Execute_Should_ApplyPolicy_When_EnclosingDeferIsDisabled(bool allow, string cost)
    {
        // arrange
        var executor = await CreateExecutorAsync([Schema], allow, out var recorder);
        var request = CreateRequest(
                "query($d: Boolean!) { product { id ... @defer(if: $d) { price ... @defer { cost } } } }",
                Authenticated())
            .SetVariableValues("""{"d":false}""")
            .Build();

        // act
        await using var result = await executor.ExecuteAsync(request, TestContext.Current.CancellationToken);
        var payloads = await ReadPayloadsAsync(result, TestContext.Current.CancellationToken);

        // assert
        payloads.MatchInlineSnapshots(
            [
                """
                {
                  "data": {
                    "product": {
                      "id": "1",
                      "price": 10
                    }
                  },
                  "pending": [
                    {
                      "id": "1",
                      "path": [
                        "product"
                      ]
                    }
                  ],
                  "hasNext": true
                }
                """,
                $$"""
                {
                  "incremental": [
                    {
                      "id": "1",
                      "data": {
                        "cost": {{cost}}
                      }
                    }
                  ],
                  "completed": [
                    {
                      "id": "1"
                    }
                  ],
                  "hasNext": false
                }
                """
            ]);
        Assert.Single(recorder.Records);
    }

    [Theory]
    [InlineData(true, "5")]
    [InlineData(false, "null")]
    public async Task Execute_Should_ApplyPolicy_When_EnclosingConditionalDeferIsDisabled(bool allow, string cost)
    {
        // arrange
        var executor = await CreateExecutorAsync([Schema], allow, out var recorder);
        var request = CreateRequest(
                """
                query($d: Boolean!, $e: Boolean!) {
                  product { id ... @defer(if: $d) { price ... @defer(if: $e) { cost } } }
                }
                """,
                Authenticated())
            .SetVariableValues("""{"d":false,"e":true}""")
            .Build();

        // act
        await using var result = await executor.ExecuteAsync(request, TestContext.Current.CancellationToken);
        var payloads = await ReadPayloadsAsync(result, TestContext.Current.CancellationToken);

        // assert
        payloads.MatchInlineSnapshots(
            [
                """
                {
                  "data": {
                    "product": {
                      "id": "1",
                      "price": 10
                    }
                  },
                  "pending": [
                    {
                      "id": "1",
                      "path": [
                        "product"
                      ]
                    }
                  ],
                  "hasNext": true
                }
                """,
                $$"""
                {
                  "incremental": [
                    {
                      "id": "1",
                      "data": {
                        "cost": {{cost}}
                      }
                    }
                  ],
                  "completed": [
                    {
                      "id": "1"
                    }
                  ],
                  "hasNext": false
                }
                """
            ]);
        Assert.Single(recorder.Records);
    }

    [Theory]
    [InlineData(true, "5")]
    [InlineData(false, "null")]
    public async Task Execute_Should_ApplyPolicy_When_NestedConditionalDeferIsDisabled(bool allow, string cost)
    {
        // arrange
        var executor = await CreateExecutorAsync([Schema], allow, out var recorder);
        var request = CreateRequest(
                """
                query($d: Boolean!, $e: Boolean!) {
                  product { id ... @defer(if: $d) { price ... @defer(if: $e) { cost } } }
                }
                """,
                Authenticated())
            .SetVariableValues("""{"d":true,"e":false}""")
            .Build();

        // act
        await using var result = await executor.ExecuteAsync(request, TestContext.Current.CancellationToken);
        var payloads = await ReadPayloadsAsync(result, TestContext.Current.CancellationToken);

        // assert
        // the deferred fields arrive in two payloads whose order depends on which fetch completes first
        Assert.Equal(3, payloads.Count);
        payloads[0].MatchInlineSnapshot(
            """
            {
              "data": {
                "product": {
                  "id": "1"
                }
              },
              "pending": [
                {
                  "id": "0",
                  "path": [
                    "product"
                  ]
                }
              ],
              "hasNext": true
            }
            """);
        MergeSubsequentPayloads(payloads).MatchInlineSnapshot(
            $$"""
            {
              "incremental": [
                {
                  "id": "0",
                  "data": {
                    "cost": {{cost}}
                  }
                },
                {
                  "id": "0",
                  "data": {
                    "price": 10
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
        Assert.Single(recorder.Records);
    }

    [Theory]
    [InlineData(true, "5")]
    [InlineData(false, "null")]
    public async Task Execute_Should_ApplyPolicy_When_MiddleDeferIsDisabled(bool allow, string cost)
    {
        // arrange
        var executor = await CreateExecutorAsync([Schema], allow, out var recorder);
        var request = CreateRequest(
                """
                query($d: Boolean!) {
                  product { id ... @defer { price ... @defer(if: $d) { ... @defer { cost } } } }
                }
                """,
                Authenticated())
            .SetVariableValues("""{"d":false}""")
            .Build();

        // act
        await using var result = await executor.ExecuteAsync(request, TestContext.Current.CancellationToken);
        var payloads = await ReadPayloadsAsync(result, TestContext.Current.CancellationToken);

        // assert
        payloads.MatchInlineSnapshots(
            [
                """
                {
                  "data": {
                    "product": {
                      "id": "1"
                    }
                  },
                  "pending": [
                    {
                      "id": "0",
                      "path": [
                        "product"
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
                      "id": "2",
                      "path": [
                        "product"
                      ]
                    }
                  ],
                  "incremental": [
                    {
                      "id": "0",
                      "data": {
                        "price": 10
                      }
                    }
                  ],
                  "completed": [
                    {
                      "id": "0"
                    }
                  ],
                  "hasNext": true
                }
                """,
                $$"""
                {
                  "incremental": [
                    {
                      "id": "2",
                      "data": {
                        "cost": {{cost}}
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
        Assert.Single(recorder.Records);
    }

    [Theory]
    [InlineData(true, "5")]
    [InlineData(false, "null")]
    public async Task Execute_Should_ApplyPolicy_When_EnclosingDeferIsDisabledInMutation(bool allow, string cost)
    {
        // arrange
        var executor = await CreateExecutorAsync([ProductSchema, ProductDetailsSchema], allow, out var recorder);
        var request = CreateRequest(
                """
                mutation($d: Boolean!) {
                  updateProduct { id ... @defer(if: $d) { price ... @defer { stock cost } } }
                }
                """,
                Authenticated())
            .SetVariableValues("""{"d":false}""")
            .Build();

        // act
        await using var result = await executor.ExecuteAsync(request, TestContext.Current.CancellationToken);
        var payloads = await ReadPayloadsAsync(result, TestContext.Current.CancellationToken);

        // assert
        payloads.MatchInlineSnapshots(
            [
                """
                {
                  "data": {
                    "updateProduct": {
                      "id": "1",
                      "price": 10
                    }
                  },
                  "pending": [
                    {
                      "id": "1",
                      "path": [
                        "updateProduct"
                      ]
                    }
                  ],
                  "hasNext": true
                }
                """,
                $$"""
                {
                  "incremental": [
                    {
                      "id": "1",
                      "data": {
                        "stock": 7,
                        "cost": {{cost}}
                      }
                    }
                  ],
                  "completed": [
                    {
                      "id": "1"
                    }
                  ],
                  "hasNext": false
                }
                """
            ]);
        Assert.Single(recorder.Records);
    }

    [Theory]
    [InlineData(true, "5")]
    [InlineData(false, "null")]
    public async Task Execute_Should_ApplyPolicy_When_EnclosingConditionalDeferIsDisabledInMutation(
        bool allow,
        string cost)
    {
        // arrange
        var executor = await CreateExecutorAsync([ProductSchema, ProductDetailsSchema], allow, out var recorder);
        var request = CreateRequest(
                """
                mutation($d: Boolean!, $e: Boolean!) {
                  updateProduct { id ... @defer(if: $d) { price ... @defer(if: $e) { stock cost } } }
                }
                """,
                Authenticated())
            .SetVariableValues("""{"d":false,"e":true}""")
            .Build();

        // act
        await using var result = await executor.ExecuteAsync(request, TestContext.Current.CancellationToken);
        var payloads = await ReadPayloadsAsync(result, TestContext.Current.CancellationToken);

        // assert
        payloads.MatchInlineSnapshots(
            [
                """
                {
                  "data": {
                    "updateProduct": {
                      "id": "1",
                      "price": 10
                    }
                  },
                  "pending": [
                    {
                      "id": "1",
                      "path": [
                        "updateProduct"
                      ]
                    }
                  ],
                  "hasNext": true
                }
                """,
                $$"""
                {
                  "incremental": [
                    {
                      "id": "1",
                      "data": {
                        "stock": 7,
                        "cost": {{cost}}
                      }
                    }
                  ],
                  "completed": [
                    {
                      "id": "1"
                    }
                  ],
                  "hasNext": false
                }
                """
            ]);
        Assert.Single(recorder.Records);
    }

    [Theory]
    [InlineData(true, "5", "8")]
    [InlineData(false, "null", "null")]
    public async Task Execute_Should_ApplyPolicy_When_EnclosingDeferIsDisabledInList(
        bool allow,
        string firstCost,
        string secondCost)
    {
        // arrange
        var executor = await CreateExecutorAsync([Schema], allow, out var recorder);
        var request = CreateRequest(
                "query($d: Boolean!) { products { id ... @defer(if: $d) { price ... @defer { cost } } } }",
                Authenticated())
            .SetVariableValues("""{"d":false}""")
            .Build();

        // act
        await using var result = await executor.ExecuteAsync(request, TestContext.Current.CancellationToken);
        var payloads = await ReadPayloadsAsync(result, TestContext.Current.CancellationToken);

        // assert
        payloads.MatchInlineSnapshots(
            [
                """
                {
                  "data": {
                    "products": [
                      {
                        "id": "1",
                        "price": 10
                      },
                      {
                        "id": "2",
                        "price": 20
                      }
                    ]
                  },
                  "pending": [
                    {
                      "id": "1",
                      "path": [
                        "products"
                      ]
                    }
                  ],
                  "hasNext": true
                }
                """,
                $$"""
                {
                  "incremental": [
                    {
                      "id": "1",
                      "subPath": [
                        0
                      ],
                      "data": {
                        "cost": {{firstCost}}
                      }
                    },
                    {
                      "id": "1",
                      "subPath": [
                        1
                      ],
                      "data": {
                        "cost": {{secondCost}}
                      }
                    }
                  ],
                  "completed": [
                    {
                      "id": "1"
                    }
                  ],
                  "hasNext": false
                }
                """
            ]);
        Assert.Single(recorder.Records);
    }

    private static Task<IRequestExecutor> CreateExecutorAsync(
        string[] sourceSchemas,
        bool allow,
        out InMemoryPolicyRecorder recorder)
    {
        var client = new PolicyTestClient(Data);
        recorder = new InMemoryPolicyRecorder();

        return CreateExecutorAsync(
            sourceSchemas,
            client,
            recorder,
            policies =>
            {
                if (allow)
                {
                    policies.Allow("finance");
                }
                else
                {
                    policies.Deny("finance");
                }
            });
    }

    private static string MergeSubsequentPayloads(List<string> payloads)
    {
        var incremental = new List<JsonNode>();
        var completed = new JsonArray();
        var hasNext = true;

        foreach (var payload in payloads.Skip(1))
        {
            var result = JsonNode.Parse(payload)!.AsObject();

            if (result["incremental"] is JsonArray entries)
            {
                incremental.AddRange(entries.Select(static entry => entry!.DeepClone()));
            }

            if (result["completed"] is JsonArray completedEntries)
            {
                foreach (var entry in completedEntries)
                {
                    completed.Add(entry!.DeepClone());
                }
            }

            hasNext = result["hasNext"]!.GetValue<bool>();
        }

        var merged = new JsonObject
        {
            ["incremental"] = new JsonArray(
                [.. incremental.OrderBy(static entry => entry.ToJsonString(), StringComparer.Ordinal)]),
            ["completed"] = completed,
            ["hasNext"] = hasNext
        };

        return merged.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
    }
}
