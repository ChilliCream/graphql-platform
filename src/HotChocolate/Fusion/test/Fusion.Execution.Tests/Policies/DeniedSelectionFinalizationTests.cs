using System.Security.Claims;
using HotChocolate.Execution;
using HotChocolate.Fusion.Policies.InMemory;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Policies;

public class DeniedSelectionFinalizationTests : AuthorizationExecutionTestBase
{
    private const string Schema =
        """
        type Query {
          product: Product
          products: [Product]
          items: [Item!]
        }

        type Product {
          id: ID!
          name: String @authenticated
          price: Int @requiresScopes(scopes: [["read"]])
          margin: Int @policy(policies: [["finance"]])
          cost: Int! @policy(policies: [["finance"]])
        }

        type Item {
          id: ID!
          label: String! @authenticated
        }
        """;

    private const string Data =
        """
        {
          "product": { "id": "1", "name": "Shoe", "price": 10, "margin": 3, "cost": 5 },
          "products": [
            { "id": "1", "name": "Shoe", "price": 10, "margin": 3, "cost": 5 },
            { "id": "2", "name": "Boot", "price": 20, "margin": 4, "cost": 8 }
          ],
          "items": [
            { "id": "1", "label": "a" },
            { "id": "2", "label": "b" }
          ]
        }
        """;

    [Fact]
    public async Task Complete_Should_ReportUnauthenticated_When_DenyHandlingIsErrorAndPrincipalIsAnonymous()
    {
        // arrange
        var executor = await CreateErrorExecutorAsync();

        // act
        await using var result = await executor.ExecuteAsync(
            CreateRequest("{ product { id name price } }").Build(),
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "The current user is not authenticated.",
                  "path": [
                    "product",
                    "name"
                  ],
                  "extensions": {
                    "code": "AUTH_NOT_AUTHENTICATED"
                  }
                },
                {
                  "message": "The current user is not authenticated.",
                  "path": [
                    "product",
                    "price"
                  ],
                  "extensions": {
                    "code": "AUTH_NOT_AUTHENTICATED"
                  }
                }
              ],
              "data": {
                "product": {
                  "id": "1",
                  "name": null,
                  "price": null
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Complete_Should_ReportUnauthorized_When_DenyHandlingIsErrorAndPrincipalIsAuthenticated()
    {
        // arrange
        var executor = await CreateErrorExecutorAsync();
        var user = Authenticated(new Claim("scope", "write"));

        // act
        await using var result = await executor.ExecuteAsync(
            CreateRequest("{ product { id name price } }", user).Build(),
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "The current user is not authorized to access this resource.",
                  "path": [
                    "product",
                    "price"
                  ],
                  "extensions": {
                    "code": "AUTH_NOT_AUTHORIZED"
                  }
                }
              ],
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
    public async Task Complete_Should_ReportEveryOccurrenceWithIndexedPath_When_DeniedSelectionIsInAList()
    {
        // arrange
        var executor = await CreateErrorExecutorAsync();

        // act
        await using var result = await executor.ExecuteAsync(
            CreateRequest("{ products { id name } }").Build(),
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "The current user is not authenticated.",
                  "path": [
                    "products",
                    0,
                    "name"
                  ],
                  "extensions": {
                    "code": "AUTH_NOT_AUTHENTICATED"
                  }
                },
                {
                  "message": "The current user is not authenticated.",
                  "path": [
                    "products",
                    1,
                    "name"
                  ],
                  "extensions": {
                    "code": "AUTH_NOT_AUTHENTICATED"
                  }
                }
              ],
              "data": {
                "products": [
                  {
                    "id": "1",
                    "name": null
                  },
                  {
                    "id": "2",
                    "name": null
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task Complete_Should_ReportEveryAlias_When_DeniedFieldIsSelectedTwice()
    {
        // arrange
        var executor = await CreateErrorExecutorAsync();

        // act
        await using var result = await executor.ExecuteAsync(
            CreateRequest("{ product { first: name second: name } }").Build(),
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "The current user is not authenticated.",
                  "path": [
                    "product",
                    "first"
                  ],
                  "extensions": {
                    "code": "AUTH_NOT_AUTHENTICATED"
                  }
                },
                {
                  "message": "The current user is not authenticated.",
                  "path": [
                    "product",
                    "second"
                  ],
                  "extensions": {
                    "code": "AUTH_NOT_AUTHENTICATED"
                  }
                }
              ],
              "data": {
                "product": {
                  "first": null,
                  "second": null
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Complete_Should_PropagateNull_When_DeniedSelectionIsNonNull()
    {
        // arrange
        var executor = await CreateErrorExecutorAsync(policies => policies.Deny("finance"));
        var user = Authenticated();

        // act
        await using var result = await executor.ExecuteAsync(
            CreateRequest("{ products { id cost } }", user).Build(),
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "The current user is not authorized to access this resource.",
                  "path": [
                    "products",
                    0,
                    "cost"
                  ],
                  "extensions": {
                    "code": "AUTH_NOT_AUTHORIZED"
                  }
                },
                {
                  "message": "The current user is not authorized to access this resource.",
                  "path": [
                    "products",
                    1,
                    "cost"
                  ],
                  "extensions": {
                    "code": "AUTH_NOT_AUTHORIZED"
                  }
                }
              ],
              "data": {
                "products": [
                  null,
                  null
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task Complete_Should_NullTheList_When_DeniedSelectionIsNonNullInANonNullListElement()
    {
        // arrange
        var executor = await CreateErrorExecutorAsync();

        // act
        await using var result = await executor.ExecuteAsync(
            CreateRequest("{ items { id label } }").Build(),
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "The current user is not authenticated.",
                  "path": [
                    "items",
                    0,
                    "label"
                  ],
                  "extensions": {
                    "code": "AUTH_NOT_AUTHENTICATED"
                  }
                }
              ],
              "data": {
                "items": null
              }
            }
            """);
    }

    [Fact]
    public async Task Complete_Should_ReportGenericError_When_DenyHandlingIsNullAndSelectionIsNonNull()
    {
        // arrange
        var executor = await CreateExecutorAsync(
            Schema,
            new AuthorizationTestClient(Data),
            new InMemoryPolicyRecorder(),
            policies => policies.Deny("finance"));
        var user = Authenticated();

        // act
        await using var result = await executor.ExecuteAsync(
            CreateRequest("{ product { id cost } }", user).Build(),
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
    }

    [Fact]
    public async Task Complete_Should_NullTheFieldAndKeepTheParents_When_DenyHandlingIsErrorAndErrorHandlingModeIsNull()
    {
        // arrange
        var executor = await CreateExecutorAsync(
            Schema,
            new AuthorizationTestClient(Data),
            new InMemoryPolicyRecorder(),
            policies => policies.Deny("finance"),
            builder =>
            {
                builder.ModifyAuthorizationOptions(options => options.DenyHandling = DenyHandling.Error);
                builder.ModifyRequestOptions(options => options.DefaultErrorHandlingMode = ErrorHandlingMode.Null);
            });
        var user = Authenticated();

        // act
        await using var result = await executor.ExecuteAsync(
            CreateRequest("{ products { id cost } }", user).Build(),
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "The current user is not authorized to access this resource.",
                  "path": [
                    "products",
                    0,
                    "cost"
                  ],
                  "extensions": {
                    "code": "AUTH_NOT_AUTHORIZED"
                  }
                },
                {
                  "message": "The current user is not authorized to access this resource.",
                  "path": [
                    "products",
                    1,
                    "cost"
                  ],
                  "extensions": {
                    "code": "AUTH_NOT_AUTHORIZED"
                  }
                }
              ],
              "data": {
                "products": [
                  {
                    "id": "1",
                    "cost": null
                  },
                  {
                    "id": "2",
                    "cost": null
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task Complete_Should_NullTheFieldAndKeepTheParent_When_DenyHandlingIsNullAndErrorHandlingModeIsNull()
    {
        // arrange
        var executor = await CreateExecutorAsync(
            Schema,
            new AuthorizationTestClient(Data),
            new InMemoryPolicyRecorder(),
            policies => policies.Deny("finance"),
            builder =>
            {
                builder.ModifyAuthorizationOptions(options => options.DenyHandling = DenyHandling.Null);
                builder.ModifyRequestOptions(options => options.DefaultErrorHandlingMode = ErrorHandlingMode.Null);
            });
        var user = Authenticated();

        // act
        await using var result = await executor.ExecuteAsync(
            CreateRequest("{ product { id cost } }", user).Build(),
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
                "product": {
                  "id": "1",
                  "cost": null
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Complete_Should_NameDirectivePolicyAndScopes_When_AttributionIsEnabled()
    {
        // arrange
        var executor = await CreateExecutorAsync(
            Schema,
            new AuthorizationTestClient(Data),
            new InMemoryPolicyRecorder(),
            policies => policies.Deny("finance"),
            builder => builder.ModifyAuthorizationOptions(
                options =>
                {
                    options.DenyHandling = DenyHandling.Error;
                    options.EnableAttribution = true;
                }));
        var user = Authenticated();

        // act
        await using var result = await executor.ExecuteAsync(
            CreateRequest("{ product { price margin } }", user).Build(),
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "The current user is not authorized to access this resource.",
                  "path": [
                    "product",
                    "margin"
                  ],
                  "extensions": {
                    "code": "AUTH_NOT_AUTHORIZED",
                    "directive": "policy",
                    "policy": "finance"
                  }
                },
                {
                  "message": "The current user is not authorized to access this resource.",
                  "path": [
                    "product",
                    "price"
                  ],
                  "extensions": {
                    "code": "AUTH_NOT_AUTHORIZED",
                    "directive": "requiresScopes",
                    "requiredScopes": [
                      [
                        "read"
                      ]
                    ]
                  }
                }
              ],
              "data": {
                "product": {
                  "price": null,
                  "margin": null
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Complete_Should_ReportDeniedSelectionOfTheDeferredPayload_When_FragmentIsDeferred()
    {
        // arrange
        var executor = await CreateErrorExecutorAsync();
        var request = CreateRequest("{ product { id ... @defer { name price } } }")
            .Build();

        // act
        await using var result = await executor.ExecuteAsync(
            request,
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
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
              "incremental": [
                {
                  "id": "0",
                  "data": {
                    "name": null,
                    "price": null
                  },
                  "errors": [
                    {
                      "message": "The current user is not authenticated.",
                      "path": [
                        "product",
                        "name"
                      ],
                      "extensions": {
                        "code": "AUTH_NOT_AUTHENTICATED"
                      }
                    },
                    {
                      "message": "The current user is not authenticated.",
                      "path": [
                        "product",
                        "price"
                      ],
                      "extensions": {
                        "code": "AUTH_NOT_AUTHENTICATED"
                      }
                    }
                  ]
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

    [Fact]
    public async Task Complete_Should_ReportDeniedSelectionOfTheSequence_When_DeferIsEnabled()
    {
        // arrange
        var executor = await CreateErrorExecutorAsync();
        var request = CreateRequest(
                "query($d: Boolean!) { product { id ... @defer(if: $d) { name price } } }")
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
                      "errors": [
                        {
                          "message": "The current user is not authenticated.",
                          "path": [
                            "product",
                            "name"
                          ],
                          "extensions": {
                            "code": "AUTH_NOT_AUTHENTICATED"
                          }
                        },
                        {
                          "message": "The current user is not authenticated.",
                          "path": [
                            "product",
                            "price"
                          ],
                          "extensions": {
                            "code": "AUTH_NOT_AUTHENTICATED"
                          }
                        }
                      ],
                      "data": {
                        "name": null,
                        "price": null
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
    }

    [Fact]
    public async Task Complete_Should_ReportDeniedSelectionOfTheSequence_When_DeferIsDisabled()
    {
        // arrange
        var executor = await CreateErrorExecutorAsync();
        var request = CreateRequest(
                "query($d: Boolean!) { product { id ... @defer(if: $d) { name price } } }")
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
                  "errors": [
                    {
                      "message": "The current user is not authenticated.",
                      "path": [
                        "product",
                        "name"
                      ],
                      "extensions": {
                        "code": "AUTH_NOT_AUTHENTICATED"
                      }
                    },
                    {
                      "message": "The current user is not authenticated.",
                      "path": [
                        "product",
                        "price"
                      ],
                      "extensions": {
                        "code": "AUTH_NOT_AUTHENTICATED"
                      }
                    }
                  ],
                  "data": {
                    "product": {
                      "id": "1",
                      "name": null,
                      "price": null
                    }
                  },
                  "hasNext": false
                }
                """
            ]);
    }

    [Fact]
    public async Task Complete_Should_NullDeniedFieldOfTheDeferredPayload_When_RootSelectionsAreUnprotected()
    {
        // arrange
        var executor = await CreateErrorExecutorAsync(policies => policies.Unanswered("finance"));
        var request = CreateRequest("{ product { id ... @defer { margin } } }", Authenticated())
            .Build();

        // act
        await using var result = await executor.ExecuteAsync(
            request,
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
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
              "incremental": [
                {
                  "id": "0",
                  "data": {
                    "margin": null
                  },
                  "errors": [
                    {
                      "message": "The current user is not authorized to access this resource.",
                      "path": [
                        "product",
                        "margin"
                      ],
                      "extensions": {
                        "code": "AUTH_NOT_AUTHORIZED"
                      }
                    }
                  ]
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

    [Fact]
    public async Task Complete_Should_LeaveTheDeferredPayloadUnchanged_When_OnlyTheRootSelectionIsDenied()
    {
        // arrange
        var executor = await CreateErrorExecutorAsync();
        var request = CreateRequest("{ product { name } ... @defer { items { id } } }")
            .Build();

        // act
        await using var result = await executor.ExecuteAsync(
            request,
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "The current user is not authenticated.",
                  "path": [
                    "product",
                    "name"
                  ],
                  "extensions": {
                    "code": "AUTH_NOT_AUTHENTICATED"
                  }
                }
              ],
              "data": {
                "product": {
                  "name": null
                }
              },
              "pending": [
                {
                  "id": "0",
                  "path": []
                }
              ],
              "incremental": [
                {
                  "id": "0",
                  "data": {
                    "items": [
                      {
                        "id": "1"
                      },
                      {
                        "id": "2"
                      }
                    ]
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

    [Fact]
    public async Task Complete_Should_DenyTheSelection_When_PolicyLeavesTheEntryUnanswered()
    {
        // arrange
        var executor = await CreateErrorExecutorAsync(policies => policies.Unanswered("finance"));
        var user = Authenticated();

        // act
        await using var result = await executor.ExecuteAsync(
            CreateRequest("{ product { id margin } }", user).Build(),
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "The current user is not authorized to access this resource.",
                  "path": [
                    "product",
                    "margin"
                  ],
                  "extensions": {
                    "code": "AUTH_NOT_AUTHORIZED"
                  }
                }
              ],
              "data": {
                "product": {
                  "id": "1",
                  "margin": null
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Complete_Should_NotReportErrors_When_EveryPolicyAllows()
    {
        // arrange
        var executor = await CreateErrorExecutorAsync(policies => policies.Allow("finance"));
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
    }

    private static Task<IRequestExecutor> CreateErrorExecutorAsync(
        Action<InMemoryPolicyBuilder>? policies = null)
        => CreateExecutorAsync(
            Schema,
            new AuthorizationTestClient(Data),
            new InMemoryPolicyRecorder(),
            policies,
            builder => builder.ModifyAuthorizationOptions(
                options => options.DenyHandling = DenyHandling.Error));
}
