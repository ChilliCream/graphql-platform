using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data.Filters;

public class VariableFilterTests
{
    [Fact]
    public async Task Without_Variables()
    {
        // arrange
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
            .AddFiltering()
            .AddQueryType()
            .AddType<CollectionType>()
            .AddType<ItemType>()
            .AddTypeExtension(typeof(Query))
            .BuildSchemaAsync();

        // act
        var result = await schema.MakeExecutable().ExecuteAsync(
            """
            query Q($collectionId: UUID!) {
                collection(collectionId: $collectionId) {
                    id
                    searchItems(
                        where: {
                            latestItem: {
                                all: {
                                    completeTime: {
                                        neq: "2025-01-01T00:00:00Z"
                                    }
                                }
                            }
                        }
                    ) {
                        id
                    }
                }
            }
            """,
            variableValues: new Dictionary<string, object?>
            {
                { "collectionId", "4d6a4213-1986-4c6c-bb92-18c0b75a5a2e" }
            });

        // assert
        Assert.Null(result.ExpectOperationResult().Errors);
    }

    [Fact]
    public async Task With_Variables()
    {
        // arrange
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
            .AddFiltering()
            .AddQueryType()
            .AddType<CollectionType>()
            .AddType<ItemType>()
            .AddTypeExtension(typeof(Query))
            .BuildSchemaAsync();

        // act
        var result = await schema.MakeExecutable().ExecuteAsync(
            """
            query Q($collectionId: UUID!, $filters: CollectionFilterInput!) {
                collection(collectionId: $collectionId) {
                    id
                    searchItems(where: $filters) {
                        id
                    }
                }
            }
            """,
            variableValues: new Dictionary<string, object?>
            {
                { "collectionId", "4d6a4213-1986-4c6c-bb92-18c0b75a5a2e" },
                {
                    "filters",
                    new Dictionary<string, object?>
                    {
                        {
                            "latestItem",
                            new Dictionary<string, object?>
                            {
                                {
                                    "all",
                                    new Dictionary<string, object?>
                                    {
                                        {
                                            "completeTime",
                                            new Dictionary<string, object?>
                                            {
                                                { "neq", "2025-01-01T00:00:00Z" }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            });

        // assert
        Assert.Null(result.ExpectOperationResult().Errors);
    }

    [QueryType]
    private static class Query
    {
        [UseSingleOrDefault]
        public static IQueryable<Collection> GetCollection(Guid collectionId)
        {
            return new List<Collection>([new Collection { Id = collectionId }]).AsQueryable();
        }
    }

    private class Collection
    {
        public Guid Id { get; set; }

        public IEnumerable<Item>? Items { get; set; } = new List<Item>();
    }

    private class CollectionType : ObjectType<Collection>
    {
        protected override void Configure(IObjectTypeDescriptor<Collection> descriptor)
        {
            descriptor.Field("searchItems")
                .Type<ListType<ItemType>>()
                .UseFiltering<CollectionFilterInputType>()
                .Resolve(
                    ctx => ctx.Parent<Collection>().Items?.AsQueryable()
                        ?? Enumerable.Empty<Item>().AsQueryable());
        }
    }

    private class Item
    {
        public Guid Id { get; set; }

        public DateTime CreateTime { get; set; }

        public DateTime? CompleteTime { get; set; }
    }

    private class ItemType : ObjectType<Item>;

    private class CollectionFilterInputType : FilterInputType<Collection>
    {
        protected override void Configure(IFilterInputTypeDescriptor<Collection> descriptor)
        {
            descriptor.BindFieldsExplicitly();
            descriptor
                .Field(pa => pa.Items!.OrderByDescending(pao => pao.CreateTime).Take(1))
                .Name("latestItem")
                .Type<ListFilterInputType<ItemFilterInputType>>();
        }
    }

    private class ItemFilterInputType : FilterInputType<Item>
    {
        protected override void Configure(IFilterInputTypeDescriptor<Item> descriptor)
        {
            descriptor.BindFieldsExplicitly();
            descriptor
                .Field(pao => pao.CompleteTime)
                .Name("completeTime");
        }
    }
}
