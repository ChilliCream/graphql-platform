using HotChocolate.Fusion.Suites.ChildTypeMismatch.A;
using HotChocolate.Fusion.Suites.ChildTypeMismatch.B;

namespace HotChocolate.Fusion.Suites;

/// <summary>
/// Port of the <c>child-type-mismatch</c> suite from
/// <c>graphql-hive/federation-gateway-audit</c>. The gateway composes
/// subgraph <c>a</c> (which exposes <c>User { id @shareable }</c> without
/// a key) and subgraph <c>b</c> (which owns <c>User @key(fields: "id")</c>,
/// <c>Admin</c>, and the union <c>Account = User | Admin</c>). Tests verify
/// that union types and nested <c>similarAccounts</c> resolve correctly
/// across subgraphs.
/// </summary>
public sealed class ChildTypeMismatchTests : ComplianceTestBase
{
    protected override Task<FusionGateway> BuildGatewayAsync()
        => FusionGatewayBuilder.ComposeAsync(
            (ASubgraph.Name, ASubgraph.BuildAsync),
            (BSubgraph.Name, BSubgraph.BuildAsync));

    [Fact]
    public Task Users_And_Accounts_Resolve_Across_Subgraphs() => RunAsync(
        query: """
            {
              users {
                id
                name
              }
              accounts {
                ... on User {
                  id
                  name
                }
                ... on Admin {
                  id
                  name
                }
              }
            }
            """,
        expectedData: """
            {
              "users": [
                {
                  "id": "u1",
                  "name": "u1-name"
                }
              ],
              "accounts": [
                {
                  "id": "u1",
                  "name": "u1-name"
                },
                {
                  "id": "a1",
                  "name": "a1-name"
                }
              ]
            }
            """);

    [Fact]
    public Task NestedInternalAlias_SimilarAccounts_Resolve() => RunAsync(
        query: """
            query NestedInternalAlias {
              users {
                id
                name
              }
              accounts {
                ... on User {
                  id
                  name
                  similarAccounts {
                    ... on User {
                      id
                      name
                    }
                    ... on Admin {
                      id
                      name
                    }
                  }
                }
                ... on Admin {
                  id
                  name
                  similarAccounts {
                    ... on User {
                      id
                      name
                    }
                    ... on Admin {
                      id
                      name
                    }
                  }
                }
              }
            }
            """,
        expectedData: """
            {
              "users": [
                {
                  "id": "u1",
                  "name": "u1-name"
                }
              ],
              "accounts": [
                {
                  "id": "u1",
                  "name": "u1-name",
                  "similarAccounts": [
                    {
                      "id": "u1",
                      "name": "u1-name"
                    },
                    {
                      "id": "a1",
                      "name": "a1-name"
                    }
                  ]
                },
                {
                  "id": "a1",
                  "name": "a1-name",
                  "similarAccounts": [
                    {
                      "id": "u1",
                      "name": "u1-name"
                    },
                    {
                      "id": "a1",
                      "name": "a1-name"
                    }
                  ]
                }
              ]
            }
            """);

    [Fact]
    public Task DeeplyNestedInternalAlias_ThreeLevels_Resolve() => RunAsync(
        query: """
            query DeeplyNestedInternalAlias {
              users {
                id
                name
              }
              accounts {
                ... on User {
                  id
                  name
                  similarAccounts {
                    ... on User {
                      id
                      name
                      similarAccounts {
                        ... on User {
                          id
                          name
                        }
                        ... on Admin {
                          id
                          name
                        }
                      }
                    }
                    ... on Admin {
                      id
                      name
                      similarAccounts {
                        ... on User {
                          id
                          name
                        }
                        ... on Admin {
                          id
                          name
                        }
                      }
                    }
                  }
                }
                ... on Admin {
                  id
                  name
                  similarAccounts {
                    ... on User {
                      id
                      name
                      similarAccounts {
                        ... on User {
                          id
                          name
                        }
                        ... on Admin {
                          id
                          name
                        }
                      }
                    }
                    ... on Admin {
                      id
                      name
                      similarAccounts {
                        ... on User {
                          id
                          name
                        }
                        ... on Admin {
                          id
                          name
                        }
                      }
                    }
                  }
                }
              }
            }
            """,
        expectedData: """
            {
              "users": [
                {
                  "id": "u1",
                  "name": "u1-name"
                }
              ],
              "accounts": [
                {
                  "id": "u1",
                  "name": "u1-name",
                  "similarAccounts": [
                    {
                      "id": "u1",
                      "name": "u1-name",
                      "similarAccounts": [
                        {
                          "id": "u1",
                          "name": "u1-name"
                        },
                        {
                          "id": "a1",
                          "name": "a1-name"
                        }
                      ]
                    },
                    {
                      "id": "a1",
                      "name": "a1-name",
                      "similarAccounts": [
                        {
                          "id": "u1",
                          "name": "u1-name"
                        },
                        {
                          "id": "a1",
                          "name": "a1-name"
                        }
                      ]
                    }
                  ]
                },
                {
                  "id": "a1",
                  "name": "a1-name",
                  "similarAccounts": [
                    {
                      "id": "u1",
                      "name": "u1-name",
                      "similarAccounts": [
                        {
                          "id": "u1",
                          "name": "u1-name"
                        },
                        {
                          "id": "a1",
                          "name": "a1-name"
                        }
                      ]
                    },
                    {
                      "id": "a1",
                      "name": "a1-name",
                      "similarAccounts": [
                        {
                          "id": "u1",
                          "name": "u1-name"
                        },
                        {
                          "id": "a1",
                          "name": "a1-name"
                        }
                      ]
                    }
                  ]
                }
              ]
            }
            """);

    [Fact]
    public Task DeeplyNested_NamesOnly_ThreeLevels_Resolve() => RunAsync(
        query: """
            query DeeplyNested {
              accounts {
                ... on User {
                  name
                  similarAccounts {
                    ... on User {
                      name
                      similarAccounts {
                        ... on User {
                          name
                        }
                        ... on Admin {
                          name
                        }
                      }
                    }
                    ... on Admin {
                      name
                      similarAccounts {
                        ... on User {
                          name
                        }
                        ... on Admin {
                          name
                        }
                      }
                    }
                  }
                }
                ... on Admin {
                  name
                  similarAccounts {
                    ... on User {
                      name
                      similarAccounts {
                        ... on User {
                          name
                        }
                        ... on Admin {
                          name
                        }
                      }
                    }
                    ... on Admin {
                      name
                      similarAccounts {
                        ... on User {
                          name
                        }
                        ... on Admin {
                          name
                        }
                      }
                    }
                  }
                }
              }
            }
            """,
        expectedData: """
            {
              "accounts": [
                {
                  "name": "u1-name",
                  "similarAccounts": [
                    {
                      "name": "u1-name",
                      "similarAccounts": [
                        {
                          "name": "u1-name"
                        },
                        {
                          "name": "a1-name"
                        }
                      ]
                    },
                    {
                      "name": "a1-name",
                      "similarAccounts": [
                        {
                          "name": "u1-name"
                        },
                        {
                          "name": "a1-name"
                        }
                      ]
                    }
                  ]
                },
                {
                  "name": "a1-name",
                  "similarAccounts": [
                    {
                      "name": "u1-name",
                      "similarAccounts": [
                        {
                          "name": "u1-name"
                        },
                        {
                          "name": "a1-name"
                        }
                      ]
                    },
                    {
                      "name": "a1-name",
                      "similarAccounts": [
                        {
                          "name": "u1-name"
                        },
                        {
                          "name": "a1-name"
                        }
                      ]
                    }
                  ]
                }
              ]
            }
            """);
}
