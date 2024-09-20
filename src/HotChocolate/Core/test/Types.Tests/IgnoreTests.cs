using HotChocolate.Execution;
using HotChocolate.Tests;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

#nullable enable

namespace HotChocolate;

public class IgnoreTests
{
    [Fact]
    public async Task IgnoreOutputField()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .BuildSchemaAsync()
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Ignore_By_Name()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryType>()
            .BuildSchemaAsync()
            .MatchSnapshotAsync();
    }

    public class Query
    {
        public Bar GetBar() => new();
    }

    public class QueryType : ObjectType<Query>
    {
        protected override void Configure(IObjectTypeDescriptor<Query> descriptor)
        {
            descriptor.Field("bar").Ignore();
            descriptor.Field("foo").Resolve("foo");
        }
    }

    public class Bar
    {
        public string Baz { get; set; } = default!;

        [GraphQLIgnore]
        public (string X, string? Y) IgnoreThis() => default;
    }
}
