using System.Collections.Immutable;
using System.Reflection;
using GreenDonut.Data;
using HotChocolate.Execution;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Pagination;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data.Pagination;

public class PagingArgumentsParameterExpressionBuilderTests
{
    [Fact]
    public async Task Maps_NullOrdering_From_PagingOptions_To_PagingArguments()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddPagingArguments()
            .ModifyPagingOptions(o => o.NullOrdering = NullOrdering.NativeNullsFirst)
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync("{ ints { nodes } }");
        var operationResult = result.ExpectOperationResult();

        // assert
        Assert.Empty(operationResult.Errors);
        Assert.Equal(NullOrdering.NativeNullsFirst, Query.PagingArguments.NullOrdering);
    }

    [Fact]
    public async Task BatchResolver_Should_Map_PagingArguments_Per_Selection()
    {
        // arrange
        BatchBrandExtensions.BatchCallCount = 0;
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<BatchQuery>()
            .AddTypeExtension<BatchBrandExtensions>()
            .AddPagingArguments()
            .BuildRequestExecutorAsync();

        // act
        var result =
            await executor.ExecuteAsync(
                """
                {
                    brands {
                        name
                        small: products(first: 1) {
                            nodes {
                                name
                            }
                        }
                        large: products(first: 2) {
                            nodes {
                                name
                            }
                        }
                    }
                }
                """);

        // assert
        Assert.Equal(2, BatchBrandExtensions.BatchCallCount);
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "brands": [
                  {
                    "name": "Brand 1",
                    "small": {
                      "nodes": [
                        {
                          "name": "Brand 1 Product 1"
                        }
                      ]
                    },
                    "large": {
                      "nodes": [
                        {
                          "name": "Brand 1 Product 1"
                        },
                        {
                          "name": "Brand 1 Product 2"
                        }
                      ]
                    }
                  },
                  {
                    "name": "Brand 2",
                    "small": {
                      "nodes": [
                        {
                          "name": "Brand 2 Product 1"
                        }
                      ]
                    },
                    "large": {
                      "nodes": [
                        {
                          "name": "Brand 2 Product 1"
                        },
                        {
                          "name": "Brand 2 Product 2"
                        }
                      ]
                    }
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task BatchResolver_Should_Not_Partition_Identical_PagingArguments()
    {
        // arrange
        BatchBrandExtensions.BatchCallCount = 0;
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<BatchQuery>()
            .AddTypeExtension<BatchBrandExtensions>()
            .AddPagingArguments()
            .BuildRequestExecutorAsync();

        // act
        var result =
            await executor.ExecuteAsync(
                """
                {
                    brands {
                        name
                        small: products(first: 1) {
                            nodes {
                                name
                            }
                        }
                        alsoSmall: products(first: 1) {
                            nodes {
                                name
                            }
                        }
                    }
                }
                """);

        // assert
        Assert.Equal(1, BatchBrandExtensions.BatchCallCount);
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "brands": [
                  {
                    "name": "Brand 1",
                    "small": {
                      "nodes": [
                        {
                          "name": "Brand 1 Product 1"
                        }
                      ]
                    },
                    "alsoSmall": {
                      "nodes": [
                        {
                          "name": "Brand 1 Product 1"
                        }
                      ]
                    }
                  },
                  {
                    "name": "Brand 2",
                    "small": {
                      "nodes": [
                        {
                          "name": "Brand 2 Product 1"
                        }
                      ]
                    },
                    "alsoSmall": {
                      "nodes": [
                        {
                          "name": "Brand 2 Product 1"
                        }
                      ]
                    }
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task BatchResolver_Should_Map_PagingArguments_Per_Selection_With_PageConnection()
    {
        // arrange
        ConnectionBrandExtensions.BatchCallCount = 0;
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<ConnectionQuery>()
            .AddTypeExtension<ConnectionBrandExtensions>()
            .AddType<ConnectionProductConnectionType>()
            .AddPagingArguments()
            .BuildRequestExecutorAsync();

        // act
        var result =
            await executor.ExecuteAsync(
                """
                {
                    brands {
                        name
                        small: products(first: 1) {
                            nodes {
                                name
                            }
                        }
                        large: products(first: 2) {
                            nodes {
                                name
                            }
                        }
                    }
                }
                """);

        // assert
        Assert.Equal(2, ConnectionBrandExtensions.BatchCallCount);
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "brands": [
                  {
                    "name": "Brand 1",
                    "small": {
                      "nodes": [
                        {
                          "name": "Brand 1 Product 1"
                        }
                      ]
                    },
                    "large": {
                      "nodes": [
                        {
                          "name": "Brand 1 Product 1"
                        },
                        {
                          "name": "Brand 1 Product 2"
                        }
                      ]
                    }
                  },
                  {
                    "name": "Brand 2",
                    "small": {
                      "nodes": [
                        {
                          "name": "Brand 2 Product 1"
                        }
                      ]
                    },
                    "large": {
                      "nodes": [
                        {
                          "name": "Brand 2 Product 1"
                        },
                        {
                          "name": "Brand 2 Product 2"
                        }
                      ]
                    }
                  }
                ]
              }
            }
            """);
    }

    public class Query
    {
        public static PagingArguments PagingArguments { get; private set; }

        [UsePaging]
        public IEnumerable<int> GetInts(PagingArguments pagingArguments)
        {
            PagingArguments = pagingArguments;

            return [];
        }
    }

    public class BatchQuery
    {
        public List<BatchBrand> GetBrands()
            =>
            [
                new(1, "Brand 1"),
                new(2, "Brand 2")
            ];
    }

    [ExtendObjectType<BatchBrand>]
    public class BatchBrandExtensions
    {
        public static int BatchCallCount { get; set; }

        [UsePaging]
        [BatchResolver]
        public List<Page<BatchProduct>> GetProducts(
            [Parent] List<BatchBrand> brands,
            PagingArguments pagingArguments)
        {
            BatchCallCount++;
            var result = new List<Page<BatchProduct>>(brands.Count);
            var count = pagingArguments.First ?? 2;

            foreach (var brand in brands)
            {
                var products = Enumerable
                    .Range(1, count)
                    .Select(i => new BatchProduct(i, $"Brand {brand.Id} Product {i}"))
                    .ToImmutableArray();

                result.Add(Page<BatchProduct>.Create(
                    products,
                    hasNextPage: false,
                    hasPreviousPage: false,
                    createCursor: product => product.Id.ToString(),
                    totalCount: count));
            }

            return result;
        }
    }

    public record BatchBrand(int Id, string Name);

    public record BatchProduct(int Id, string Name);

    public class ConnectionQuery
    {
        public List<ConnectionBrand> GetBrands()
            =>
            [
                new(1, "Brand 1"),
                new(2, "Brand 2")
            ];
    }

    [ExtendObjectType<ConnectionBrand>]
    public class ConnectionBrandExtensions
    {
        public static int BatchCallCount { get; set; }

        [UseConnectionProductConnection]
        [UseConnection]
        [BatchResolver]
        public List<PageConnection<ConnectionProduct>> GetProducts(
            [Parent] List<ConnectionBrand> brands,
            PagingArguments pagingArguments)
        {
            BatchCallCount++;
            var result = new List<PageConnection<ConnectionProduct>>(brands.Count);
            var count = pagingArguments.First ?? 2;

            foreach (var brand in brands)
            {
                var products = Enumerable
                    .Range(1, count)
                    .Select(i => new ConnectionProduct(i, $"Brand {brand.Id} Product {i}"))
                    .ToImmutableArray();
                var page = Page<ConnectionProduct>.Create(
                    products,
                    hasNextPage: false,
                    hasPreviousPage: false,
                    createCursor: product => product.Id.ToString(),
                    totalCount: count);

                result.Add(new PageConnection<ConnectionProduct>(page));
            }

            return result;
        }
    }

    public record ConnectionBrand(int Id, string Name);

    public record ConnectionProduct(int Id, string Name);

    public sealed class ConnectionProductConnectionType
        : ObjectType<PageConnection<ConnectionProduct>>
    {
        protected override void Configure(
            IObjectTypeDescriptor<PageConnection<ConnectionProduct>> descriptor)
        {
            descriptor.BindFieldsExplicitly();
            descriptor.Name("ConnectionProductConnection");
            descriptor.Field(t => t.Nodes);
        }
    }

    public sealed class UseConnectionProductConnectionAttribute
        : ObjectFieldDescriptorAttribute
    {
        protected override void OnConfigure(
            IDescriptorContext context,
            IObjectFieldDescriptor descriptor,
            MemberInfo? member)
        {
            descriptor.Type<NonNullType<ConnectionProductConnectionType>>();
            descriptor.Argument("first", a => a.Type<IntType>());
            descriptor.Argument("after", a => a.Type<StringType>());
            descriptor.Argument("last", a => a.Type<IntType>());
            descriptor.Argument("before", a => a.Type<StringType>());
        }
    }
}
