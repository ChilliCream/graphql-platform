using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Security.Claims;
using HotChocolate.Execution;
using HotChocolate.Fusion.Policies.InMemory;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Policies;

public class OperationPolicyMiddlewareTests : AuthorizationExecutionTestBase
{
    private const string Schema =
        """
        type Query {
          product: Product
          products: [Product]
          secret: String @authenticated
          guarded: String @policy(policies: [["finance"]])
          either: String @policy(policies: [["editor"], ["admin"]])
          both: String @policy(policies: [["editor", "admin"]])
          productById(id: ID!): Product @policy(policies: [["owner"]])
          productByFilter(filter: ProductFilter!): Product @policy(policies: [["owner"]])
          productsByIds(ids: [ID!]!): [Product] @policy(policies: [["owner"]])
        }

        input ProductFilter {
          ownerId: ID
        }

        type Product {
          id: ID!
          name: String @authenticated
          price: Int @requiresScopes(scopes: [["read"]])
          cost: Int! @policy(policies: [["finance"]])
          margin: Int @policy(policies: [["finance"]])
        }
        """;

    private const string MutationSchema =
        """
        type Query {
          field: String
        }

        type Mutation {
          rename: String @authenticated
        }
        """;

    private const string GhostSchema =
        """
        type Query {
          ghost: String @policy(policies: [["ghost"]])
        }
        """;

    private const string Data =
        """
        {
          "product": { "id": "1", "name": "Shoe", "price": 10, "cost": 5, "margin": 3 },
          "products": [
            { "id": "1", "name": "Shoe", "price": 10, "cost": 5 },
            { "id": "2", "name": "Boot", "price": 20, "cost": 8 }
          ],
          "secret": "s3cret",
          "guarded": "g",
          "either": "e",
          "both": "b",
          "productById": { "id": "1", "name": "Shoe", "price": 10, "cost": 5 },
          "productByFilter": { "id": "1", "name": "Shoe", "price": 10, "cost": 5 },
          "productsByIds": [{ "id": "1", "name": "Shoe", "price": 10, "cost": 5 }]
        }
        """;

    [Fact]
    public async Task InvokeAsync_Should_NotEvaluatePolicies_When_OperationIsUnprotected()
    {
        // arrange
        var client = new AuthorizationTestClient(Data);
        var recorder = new InMemoryPolicyRecorder();
        var executor = await CreateExecutorAsync(
            Schema,
            client,
            recorder,
            policies => policies.Deny("finance"));

        // act
        await using var result = await executor.ExecuteAsync(
            CreateRequest("{ product { id } }").Build(),
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "product": {
                  "id": "1"
                }
              }
            }
            """);
        Assert.Empty(recorder.Records);
    }

    [Fact]
    public async Task InvokeAsync_Should_ReturnAllFields_When_EveryPolicyAllows()
    {
        // arrange
        var client = new AuthorizationTestClient(Data);
        var recorder = new InMemoryPolicyRecorder();
        var executor = await CreateExecutorAsync(
            Schema,
            client,
            recorder,
            policies => policies.Allow("finance"));
        var user = Authenticated(new Claim("scope", "read"));

        // act
        await using var result = await executor.ExecuteAsync(
            CreateRequest("{ product { id name price cost } }", user).Build(),
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "product": {
                  "id": "1",
                  "name": "Shoe",
                  "price": 10,
                  "cost": 5
                }
              }
            }
            """);
        Assert.Equal(
            ["Product.cost"],
            recorder.Records.Select(r => r.Entry.Selection.Field.Coordinate.ToString()));
    }

    [Fact]
    public async Task InvokeAsync_Should_ReturnNullWithoutError_When_DenyHandlingIsNull()
    {
        // arrange
        var client = new AuthorizationTestClient(Data);
        var executor = await CreateExecutorAsync(
            Schema,
            client,
            new InMemoryPolicyRecorder(),
            policies => policies.Allow("finance"));
        var user = Authenticated();

        // act
        await using var result = await executor.ExecuteAsync(
            CreateRequest("{ product { id name price } }", user).Build(),
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "product": {
                  "id": "1",
                  "name": "Shoe",
                  "price": null
                }
              }
            }
            """);
    }

    [Fact]
    public async Task InvokeAsync_Should_NeverReceiveDeniedData_When_PolicyDenies()
    {
        // arrange
        var client = new AuthorizationTestClient(Data);
        var executor = await CreateExecutorAsync(
            Schema,
            client,
            new InMemoryPolicyRecorder(),
            policies => policies.Allow("finance"));
        var user = Authenticated();

        // act
        await using var result = await executor.ExecuteAsync(
            CreateRequest("{ product { id price } }", user).Build(),
            TestContext.Current.CancellationToken);

        // assert
        Assert.Single(client.Requests).MatchInlineSnapshot(
            """
            query Op_915ec426_1($__fusion_policy_1: Boolean!) { product { id price @skip(if: $__fusion_policy_1) } }
            """);
    }

    [Fact]
    public async Task InvokeAsync_Should_NotCallScopeAndNamedPolicies_When_PrincipalIsMissing()
    {
        // arrange
        var client = new AuthorizationTestClient(Data);
        var recorder = new InMemoryPolicyRecorder();
        var executor = await CreateExecutorAsync(
            Schema,
            client,
            recorder,
            policies => policies.Allow("finance"));

        // act
        await using var result = await executor.ExecuteAsync(
            CreateRequest("{ product { id name price cost } }").Build(),
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "Cannot return null for non-nullable field.",
                  "path": [
                    "product",
                    "cost"
                  ],
                  "extensions": {
                    "code": "HC0018"
                  }
                }
              ],
              "data": {
                "product": null
              }
            }
            """);
        Assert.Empty(recorder.Records);
    }

    [Fact]
    public async Task InvokeAsync_Should_NotFetch_When_EveryRootSelectionOfTheNodeIsDenied()
    {
        // arrange
        var client = new AuthorizationTestClient(Data);
        var executor = await CreateExecutorAsync(
            Schema,
            client,
            new InMemoryPolicyRecorder());

        // act
        await using var result = await executor.ExecuteAsync(
            CreateRequest("{ secret }").Build(),
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "secret": null
              }
            }
            """);
        Assert.Empty(client.Requests);
    }

    [Theory]
    [InlineData(true, true, """
        {
          "data": {
            "either": "e",
            "both": "b"
          }
        }
        """)]
    [InlineData(true, false, """
        {
          "data": {
            "either": "e",
            "both": null
          }
        }
        """)]
    [InlineData(false, true, """
        {
          "data": {
            "either": "e",
            "both": null
          }
        }
        """)]
    [InlineData(false, false, """
        {
          "data": {
            "either": null,
            "both": null
          }
        }
        """)]
    public async Task InvokeAsync_Should_CombinePolicyGroups_When_PoliciesAreAlternativesAndConjunctions(
        bool editorAllows,
        bool adminAllows,
        string expected)
    {
        // arrange
        var client = new AuthorizationTestClient(Data);
        var executor = await CreateExecutorAsync(
            Schema,
            client,
            new InMemoryPolicyRecorder(),
            policies => policies
                .Evaluate("editor", (_, _) => editorAllows)
                .Evaluate("admin", (_, _) => adminAllows));
        var user = Authenticated();

        // act
        await using var result = await executor.ExecuteAsync(
            CreateRequest("{ either both }", user).Build(),
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(expected);
    }

    [Fact]
    public async Task InvokeAsync_Should_NotExecuteMutation_When_RootFieldIsDenied()
    {
        // arrange
        var client = new AuthorizationTestClient("""{"rename":"done"}""");
        var executor = await CreateExecutorAsync(
            MutationSchema,
            client,
            new InMemoryPolicyRecorder());

        // act
        await using var result = await executor.ExecuteAsync(
            CreateRequest("mutation { rename }").Build(),
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "rename": null
              }
            }
            """);
        Assert.Empty(client.Requests);
    }

    [Fact]
    public async Task InvokeAsync_Should_ReturnGenericError_When_PolicyIsNotResolved()
    {
        // arrange
        var client = new AuthorizationTestClient("""{"ghost":"gh"}""");
        var listener = new CapturingExecutionDiagnosticEventListener();
        var executor = await CreateExecutorAsync(
            GhostSchema,
            client,
            new InMemoryPolicyRecorder(),
            configure: builder =>
            {
                builder.ModifyAuthorizationOptions(options => options.DisableAuthorizationValidation = true);
                builder.AddDiagnosticEventListener(_ => listener);
            });
        var user = Authenticated();

        // act
        await using var result = await executor.ExecuteAsync(
            CreateRequest("{ ghost }", user).Build(),
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "The authorization of the request could not be evaluated."
                }
              ]
            }
            """);
        Assert.Equal(
            HttpStatusCode.InternalServerError,
            result.ExpectOperationResult().ContextData[ExecutionContextData.HttpStatusCode]);
        Assert.Empty(client.Requests);
        Assert.Equal(
            "No policy provider knows the policy 'ghost' of the directive '@policy'.",
            Assert.Single(listener.RequestErrors).Message);
    }

    [Fact]
    public async Task InvokeAsync_Should_ReturnGenericError_When_PolicyIsNotResolvedAndPrincipalIsAnonymous()
    {
        // arrange
        var client = new AuthorizationTestClient("""{"ghost":"gh"}""");
        var listener = new CapturingExecutionDiagnosticEventListener();
        var executor = await CreateExecutorAsync(
            GhostSchema,
            client,
            new InMemoryPolicyRecorder(),
            configure: builder =>
            {
                builder.ModifyAuthorizationOptions(options => options.DisableAuthorizationValidation = true);
                builder.AddDiagnosticEventListener(_ => listener);
            });

        // act
        await using var result = await executor.ExecuteAsync(
            CreateRequest("{ ghost }").Build(),
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "The authorization of the request could not be evaluated."
                }
              ]
            }
            """);
        Assert.Equal(
            HttpStatusCode.InternalServerError,
            result.ExpectOperationResult().ContextData[ExecutionContextData.HttpStatusCode]);
        Assert.Empty(client.Requests);
        Assert.Equal(
            "No policy provider knows the policy 'ghost' of the directive '@policy'.",
            Assert.Single(listener.RequestErrors).Message);
    }

    [Fact]
    public async Task InvokeAsync_Should_SkipEvaluation_When_RequestIsWarmup()
    {
        // arrange
        var client = new AuthorizationTestClient(Data);
        var recorder = new InMemoryPolicyRecorder();
        var executor = await CreateExecutorAsync(
            Schema,
            client,
            recorder,
            policies => policies.Deny("finance"));
        var request = CreateRequest("{ product { id cost } }").MarkAsWarmupRequest().Build();

        // act
        await using var result = await executor.ExecuteAsync(
            request,
            TestContext.Current.CancellationToken);

        // assert
        Assert.IsType<WarmupExecutionResult>(result);
        Assert.Empty(recorder.Records);
        Assert.Empty(client.Requests);
    }

    [Fact]
    public async Task InvokeAsync_Should_EvaluateEachVariableSetSeparately_When_RequestIsBatched()
    {
        // arrange
        var client = new AuthorizationTestClient(Data);
        var executor = await CreateExecutorAsync(
            Schema,
            client,
            new InMemoryPolicyRecorder(),
            policies => policies.Evaluate(
                "owner",
                (_, entry) => entry.Arguments["id"] is StringValueNode { Value: "1" }));
        var request = CreateRequest("query($id: ID!) { productById(id: $id) { id } }", Authenticated())
            .SetVariableValues("""[{"id":"1"},{"id":"2"}]""")
            .Build();

        // act
        await using var result = await executor.ExecuteAsync(
            request,
            TestContext.Current.CancellationToken);

        // assert
        result.ExpectOperationResultBatch().Results.MatchInlineSnapshots(
            [
                """
                {
                  "data": {
                    "productById": {
                      "id": "1"
                    }
                  }
                }
                """,
                """
                {
                  "data": {
                    "productById": null
                  }
                }
                """
            ]);
    }

    [Fact]
    public async Task InvokeAsync_Should_NotShareDecisions_When_OperationWithoutVariablesRunsTwice()
    {
        // arrange
        var client = new AuthorizationTestClient(Data);
        var executor = await CreateExecutorAsync(
            Schema,
            client,
            new InMemoryPolicyRecorder(),
            policies => policies.Evaluate(
                "finance",
                (context, _) => context.User.HasClaim("role", "finance")));
        var denied = Authenticated();
        var allowed = Authenticated(new Claim("role", "finance"));

        // act
        await using var deniedResult = await executor.ExecuteAsync(
            CreateRequest("{ product { id margin } }", denied).Build(),
            TestContext.Current.CancellationToken);
        await using var allowedResult = await executor.ExecuteAsync(
            CreateRequest("{ product { id margin } }", allowed).Build(),
            TestContext.Current.CancellationToken);

        // assert
        deniedResult.MatchInlineSnapshot(
            """
            {
              "data": {
                "product": {
                  "id": "1",
                  "margin": null
                }
              }
            }
            """);
        allowedResult.MatchInlineSnapshot(
            """
            {
              "data": {
                "product": {
                  "id": "1",
                  "margin": 3
                }
              }
            }
            """);
    }

    [Fact]
    public async Task InvokeAsync_Should_CopyVariables_When_VariableCollectionIsCustom()
    {
        // arrange
        var client = new AuthorizationTestClient(Data);
        var executor = await CreateExecutorAsync(
            Schema,
            client,
            new InMemoryPolicyRecorder(),
            policies => policies.Evaluate(
                "owner",
                (_, entry) => entry.Arguments["id"] is StringValueNode { Value: "1" }),
            builder => builder.UseRequest(
                next => context =>
                {
                    var wrapped = new IVariableValueCollection[context.VariableValues.Length];

                    for (var i = 0; i < wrapped.Length; i++)
                    {
                        wrapped[i] = new ReadOnlyVariableValues(context.VariableValues[i]);
                    }

                    context.VariableValues = [.. wrapped];
                    return next(context);
                },
                key: "CustomVariables",
                before: "FusionOperationPolicyMiddleware"));
        var request = CreateRequest("query($id: ID!) { productById(id: $id) { id } }", Authenticated())
            .SetVariableValues("""[{"id":"1"},{"id":"2"}]""")
            .Build();

        // act
        await using var result = await executor.ExecuteAsync(
            request,
            TestContext.Current.CancellationToken);

        // assert
        result.ExpectOperationResultBatch().Results.MatchInlineSnapshots(
            [
                """
                {
                  "data": {
                    "productById": {
                      "id": "1"
                    }
                  }
                }
                """,
                """
                {
                  "data": {
                    "productById": null
                  }
                }
                """
            ]);
    }

    [Fact]
    public async Task InvokeAsync_Should_PassResolvedArguments_When_VariableIsNestedInAnInputObject()
    {
        // arrange
        var client = new AuthorizationTestClient(Data);
        var executor = await CreateExecutorAsync(
            Schema,
            client,
            new InMemoryPolicyRecorder(),
            policies => policies.Evaluate(
                "owner",
                (_, entry) => entry.Arguments["filter"] is ObjectValueNode
                {
                    Fields: [{ Value: StringValueNode { Value: "1" } }]
                }));
        var request = CreateRequest(
                "query($id: ID!) { productByFilter(filter: { ownerId: $id }) { id } }",
                Authenticated())
            .SetVariableValues("""[{"id":"1"},{"id":"2"}]""")
            .Build();

        // act
        await using var result = await executor.ExecuteAsync(
            request,
            TestContext.Current.CancellationToken);

        // assert
        result.ExpectOperationResultBatch().Results.MatchInlineSnapshots(
            [
                """
                {
                  "data": {
                    "productByFilter": {
                      "id": "1"
                    }
                  }
                }
                """,
                """
                {
                  "data": {
                    "productByFilter": null
                  }
                }
                """
            ]);
    }

    [Fact]
    public async Task InvokeAsync_Should_PassResolvedArguments_When_VariableIsNestedInAList()
    {
        // arrange
        var client = new AuthorizationTestClient(Data);
        var executor = await CreateExecutorAsync(
            Schema,
            client,
            new InMemoryPolicyRecorder(),
            policies => policies.Evaluate(
                "owner",
                (_, entry) => entry.Arguments["ids"] is ListValueNode
                {
                    Items: [StringValueNode { Value: "1" }]
                }));
        var request = CreateRequest(
                "query($id: ID!) { productsByIds(ids: [$id]) { id } }",
                Authenticated())
            .SetVariableValues("""[{"id":"1"},{"id":"2"}]""")
            .Build();

        // act
        await using var result = await executor.ExecuteAsync(
            request,
            TestContext.Current.CancellationToken);

        // assert
        result.ExpectOperationResultBatch().Results.MatchInlineSnapshots(
            [
                """
                {
                  "data": {
                    "productsByIds": [
                      {
                        "id": "1"
                      }
                    ]
                  }
                }
                """,
                """
                {
                  "data": {
                    "productsByIds": null
                  }
                }
                """
            ]);
    }

    [Theory]
    [InlineData(true, 0, """
        {
          "data": {
            "product": {
              "id": "1"
            }
          }
        }
        """)]
    [InlineData(false, 1, """
        {
          "data": {
            "product": {
              "id": "1"
            },
            "guarded": "g"
          }
        }
        """)]
    public async Task InvokeAsync_Should_OnlyEvaluateIncludedSelections_When_SelectionIsSkippedByTheClient(
        bool skip,
        int expectedRecords,
        string expected)
    {
        // arrange
        var client = new AuthorizationTestClient(Data);
        var recorder = new InMemoryPolicyRecorder();
        var executor = await CreateExecutorAsync(
            Schema,
            client,
            recorder,
            policies => policies.Allow("finance"));
        var request = CreateRequest("query($skip: Boolean!) { guarded @skip(if: $skip) product { id } }", Authenticated())
            .SetVariableValues($$"""{"skip":{{(skip ? "true" : "false")}}}""")
            .Build();

        // act
        await using var result = await executor.ExecuteAsync(
            request,
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(expected);
        Assert.Equal(expectedRecords, recorder.Records.Count);
    }

    [Theory]
    [InlineData(true, """
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
          "incremental": [
            {
              "id": "0",
              "data": {
                "margin": 3
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

        """)]
    [InlineData(false, """
        {
          "data": {
            "product": {
              "id": "1",
              "margin": 3
            }
          },
          "hasNext": false
        }
        """)]
    public async Task InvokeAsync_Should_EvaluateEachOccurrenceOnce_When_DeferIsConditional(
        bool defer,
        string expected)
    {
        // arrange
        var client = new AuthorizationTestClient(Data);
        var recorder = new InMemoryPolicyRecorder();
        var executor = await CreateExecutorAsync(
            Schema,
            client,
            recorder,
            policies => policies.Allow("finance"));
        var request = CreateRequest(
                "query($d: Boolean!) { product { id ... @defer(if: $d) { margin } } }",
                Authenticated())
            .SetVariableValues($$"""{"d":{{(defer ? "true" : "false")}}}""")
            .Build();

        // act
        await using var result = await executor.ExecuteAsync(
            request,
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(expected);
        Assert.Single(recorder.Records);
    }

    [Fact]
    public async Task InvokeAsync_Should_SkipTheDeniedSelectionOfEveryFetch_When_DeferIsEnabled()
    {
        // arrange
        var client = new AuthorizationTestClient(Data);
        var recorder = new InMemoryPolicyRecorder();
        var executor = await CreateExecutorAsync(
            Schema,
            client,
            recorder,
            policies => policies.Deny("finance"));
        var request = CreateRequest(
                "query($d: Boolean!) { product { id ... @defer(if: $d) { margin } } }",
                Authenticated())
            .SetVariableValues("""{"d":true}""")
            .Build();

        // act
        await using var result = await executor.ExecuteAsync(
            request,
            TestContext.Current.CancellationToken);
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
                  "incremental": [
                    {
                      "id": "0",
                      "data": {
                        "margin": null
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
                """
            ]);
        client.Requests.MatchInlineSnapshots(
            [
                """
                query Op_98828f15_1($d: Boolean!, $__fusion_policy_2: Boolean!) { product { id ... @skip(if: $d) { margin @skip(if: $__fusion_policy_2) } } }
                """,
                """
                query Op_defer_1($__fusion_policy_1: Boolean!) { product { margin @skip(if: $__fusion_policy_1) } }
                """
            ]);
        Assert.Single(recorder.Records);
    }

    [Fact]
    public async Task InvokeAsync_Should_SkipTheDeniedSelectionOfEveryFetch_When_DeferIsDisabled()
    {
        // arrange
        var client = new AuthorizationTestClient(Data);
        var recorder = new InMemoryPolicyRecorder();
        var executor = await CreateExecutorAsync(
            Schema,
            client,
            recorder,
            policies => policies.Deny("finance"));
        var request = CreateRequest(
                "query($d: Boolean!) { product { id ... @defer(if: $d) { margin } } }",
                Authenticated())
            .SetVariableValues("""{"d":false}""")
            .Build();

        // act
        await using var result = await executor.ExecuteAsync(
            request,
            TestContext.Current.CancellationToken);
        var payloads = await ReadPayloadsAsync(result, TestContext.Current.CancellationToken);

        // assert
        payloads.MatchInlineSnapshots(
            [
                """
                {
                  "data": {
                    "product": {
                      "id": "1",
                      "margin": null
                    }
                  },
                  "hasNext": false
                }
                """
            ]);
        client.Requests.MatchInlineSnapshots(
            [
                """
                query Op_98828f15_1($d: Boolean!, $__fusion_policy_2: Boolean!) { product { id ... @skip(if: $d) { margin @skip(if: $__fusion_policy_2) } } }
                """
            ]);
        Assert.Single(recorder.Records);
    }

    private sealed class ReadOnlyVariableValues(IVariableValueCollection inner) : IVariableValueCollection
    {
        public bool IsEmpty => inner.IsEmpty;

        public T GetValue<T>(string name) where T : IValueNode
            => inner.GetValue<T>(name);

        public bool TryGetValue<T>(string name, [NotNullWhen(true)] out T? value) where T : IValueNode
            => inner.TryGetValue(name, out value);

        public IEnumerator<VariableValue> GetEnumerator()
            => inner.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();
    }
}
