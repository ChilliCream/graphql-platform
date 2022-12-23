using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Data.Sorting;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data.MySample;

public class FunkySort
{
    [Fact]
    public async Task Foo()
    {
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddSorting()
            .BuildRequestExecutorAsync();

        var res1 = await executor.ExecuteAsync(
            QueryRequestBuilder
                .New()
                .SetQuery("{ items(first: 1) { nodes { id name } } }")
                .Create());

        // TODO assert
    }

    public class Query
    {
        private static readonly List<RootType> s_items = new()
        {
            new RootType
            {
                Id = Guid.NewGuid(),
                Name = "John doe",
                Nested = new NestedType { Items = new[] { "Item 1", "Item 2" } }
            },
            new RootType
            {
                Id = Guid.NewGuid(),
                Name = "Sue Likely",
                Nested = new NestedType { Items = new[] { "Contract A", "Contract B" } }
            }
        };

        [UsePaging]
        [UseSorting]
        public IExecutable<RootType> GetItems() => s_items.AsQueryable().AsExecutable();
    }

    public class RootType
    {
        [GraphQLType(typeof(NonNullType<IdType>))]
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public NestedType? Nested { get; set; }
    }

    [GraphQLIgnoreSort]
    public class NestedType
    {
        public IEnumerable<string> Items { get; set; } = Enumerable.Empty<string>();
    }
}
