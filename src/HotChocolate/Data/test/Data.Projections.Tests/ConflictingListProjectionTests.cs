using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data;

public class ConflictingListProjectionTests
{
    [Fact]
    public async Task Projection_Should_PreserveSelections_When_ListHasDifferentFiltering()
    {
        // arrange
        var executor = await CreateExecutorAsync(false);

        // act
        var results = new[]
        {
            await executor.ExecuteAsync(
                "{ books { a: reviews(where: { rating: { eq: 4 } }) { text } b: reviews { rating } } }",
                TestContext.Current.CancellationToken),
            await executor.ExecuteAsync(
                "{ books { b: reviews { rating } a: reviews(where: { rating: { eq: 4 } }) { text } } }",
                TestContext.Current.CancellationToken)
        };

        // assert
        results.Select(t => t.ToJson()).MatchInlineSnapshots(
        [
            """
            {
              "data": {
                "books": [
                  {
                    "a": [
                      {
                        "text": "Great"
                      }
                    ],
                    "b": [
                      {
                        "rating": 4
                      },
                      {
                        "rating": 2
                      }
                    ]
                  }
                ]
              }
            }
            """,
            """
            {
              "data": {
                "books": [
                  {
                    "b": [
                      {
                        "rating": 4
                      },
                      {
                        "rating": 2
                      }
                    ],
                    "a": [
                      {
                        "text": "Great"
                      }
                    ]
                  }
                ]
              }
            }
            """
        ]);
    }

    [Fact]
    public async Task Projection_Should_PreserveSelections_When_ListHasTwoFilters()
    {
        // arrange
        var executor = await CreateExecutorAsync(false);

        // act
        var results = new[]
        {
            await executor.ExecuteAsync(
                "{ books { a: reviews(where: { rating: { eq: 4 } }) { text } b: reviews(where: { rating: { eq: 2 } }) { text } } }",
                TestContext.Current.CancellationToken),
            await executor.ExecuteAsync(
                "{ books { b: reviews(where: { rating: { eq: 2 } }) { text } a: reviews(where: { rating: { eq: 4 } }) { text } } }",
                TestContext.Current.CancellationToken)
        };

        // assert
        results.Select(t => t.ToJson()).MatchInlineSnapshots(
        [
            """
            {
              "data": {
                "books": [
                  {
                    "a": [
                      {
                        "text": "Great"
                      }
                    ],
                    "b": [
                      {
                        "text": "Fine"
                      }
                    ]
                  }
                ]
              }
            }
            """,
            """
            {
              "data": {
                "books": [
                  {
                    "b": [
                      {
                        "text": "Fine"
                      }
                    ],
                    "a": [
                      {
                        "text": "Great"
                      }
                    ]
                  }
                ]
              }
            }
            """
        ]);
    }

    [Fact]
    public async Task Projection_Should_PreserveSelections_When_ListHasDifferentSorting()
    {
        // arrange
        var executor = await CreateExecutorAsync(false);

        // act
        var results = new[]
        {
            await executor.ExecuteAsync(
                "{ books { a: reviews(order: { rating: ASC }) { text } b: reviews { text } } }",
                TestContext.Current.CancellationToken),
            await executor.ExecuteAsync(
                "{ books { b: reviews { text } a: reviews(order: { rating: ASC }) { text } } }",
                TestContext.Current.CancellationToken)
        };

        // assert
        results.Select(t => t.ToJson()).MatchInlineSnapshots(
        [
            """
            {
              "data": {
                "books": [
                  {
                    "a": [
                      {
                        "text": "Fine"
                      },
                      {
                        "text": "Great"
                      }
                    ],
                    "b": [
                      {
                        "text": "Great"
                      },
                      {
                        "text": "Fine"
                      }
                    ]
                  }
                ]
              }
            }
            """,
            """
            {
              "data": {
                "books": [
                  {
                    "b": [
                      {
                        "text": "Great"
                      },
                      {
                        "text": "Fine"
                      }
                    ],
                    "a": [
                      {
                        "text": "Fine"
                      },
                      {
                        "text": "Great"
                      }
                    ]
                  }
                ]
              }
            }
            """
        ]);
    }

    [Fact]
    public async Task Projection_Should_PreserveSelections_When_ListHasTwoSorts()
    {
        // arrange
        var executor = await CreateExecutorAsync(false);

        // act
        var results = new[]
        {
            await executor.ExecuteAsync(
                "{ books { a: reviews(order: { rating: ASC }) { text } b: reviews(order: { rating: DESC }) { text } } }",
                TestContext.Current.CancellationToken),
            await executor.ExecuteAsync(
                "{ books { b: reviews(order: { rating: DESC }) { text } a: reviews(order: { rating: ASC }) { text } } }",
                TestContext.Current.CancellationToken)
        };

        // assert
        results.Select(t => t.ToJson()).MatchInlineSnapshots(
        [
            """
            {
              "data": {
                "books": [
                  {
                    "a": [
                      {
                        "text": "Fine"
                      },
                      {
                        "text": "Great"
                      }
                    ],
                    "b": [
                      {
                        "text": "Great"
                      },
                      {
                        "text": "Fine"
                      }
                    ]
                  }
                ]
              }
            }
            """,
            """
            {
              "data": {
                "books": [
                  {
                    "b": [
                      {
                        "text": "Great"
                      },
                      {
                        "text": "Fine"
                      }
                    ],
                    "a": [
                      {
                        "text": "Fine"
                      },
                      {
                        "text": "Great"
                      }
                    ]
                  }
                ]
              }
            }
            """
        ]);
    }

    [Fact]
    public async Task Projection_Should_PreserveSelections_When_ListHasRequiredFilteredList()
    {
        // arrange
        var executor = await CreateExecutorAsync(true);

        // act
        var results = new[]
        {
            await executor.ExecuteAsync(
                "{ books { a: reviews(where: { rating: { eq: 4 } }) { text } averageRating } }",
                TestContext.Current.CancellationToken),
            await executor.ExecuteAsync(
                "{ books { averageRating a: reviews(where: { rating: { eq: 4 } }) { text } } }",
                TestContext.Current.CancellationToken)
        };

        // assert
        results.Select(t => t.ToJson()).MatchInlineSnapshots(
        [
            """
            {
              "data": {
                "books": [
                  {
                    "a": [
                      {
                        "text": "Great"
                      }
                    ],
                    "averageRating": 3
                  }
                ]
              }
            }
            """,
            """
            {
              "data": {
                "books": [
                  {
                    "averageRating": 3,
                    "a": [
                      {
                        "text": "Great"
                      }
                    ]
                  }
                ]
              }
            }
            """
        ]);
    }

    [Fact]
    public async Task Projection_Should_PreserveSelections_When_ListHasRequiredSortedList()
    {
        // arrange
        var executor = await CreateExecutorAsync(true);

        // act
        var results = new[]
        {
            await executor.ExecuteAsync(
                "{ books { a: reviews(order: { rating: ASC }) { text } averageRating } }",
                TestContext.Current.CancellationToken),
            await executor.ExecuteAsync(
                "{ books { averageRating a: reviews(order: { rating: ASC }) { text } } }",
                TestContext.Current.CancellationToken)
        };

        // assert
        results.Select(t => t.ToJson()).MatchInlineSnapshots(
        [
            """
            {
              "data": {
                "books": [
                  {
                    "a": [
                      {
                        "text": "Fine"
                      },
                      {
                        "text": "Great"
                      }
                    ],
                    "averageRating": 3
                  }
                ]
              }
            }
            """,
            """
            {
              "data": {
                "books": [
                  {
                    "averageRating": 3,
                    "a": [
                      {
                        "text": "Fine"
                      },
                      {
                        "text": "Great"
                      }
                    ]
                  }
                ]
              }
            }
            """
        ]);
    }

    private static ValueTask<IRequestExecutor> CreateExecutorAsync(bool includeRequirements)
    {
        var builder = new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<MergedParentRequiresQuery>()
            .AddProjections()
            .AddFiltering()
            .AddSorting();

        if (includeRequirements)
        {
            builder.AddTypeExtension(typeof(ListParentRequiresBookExtensions));
        }

        return builder.BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);
    }
}
