#nullable enable

using HotChocolate.Execution;
using HotChocolate.Tests;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate;

public class SemanticNonNullTests
{
    [Fact]
    public async Task Test()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .ModifyOptions(o => o.EnableSemanticNonNull = true)
            .AddQueryType<Query>()
            .BuildSchemaAsync()
            .MatchSnapshotAsync();
    }

     public class Query
    {
        [GraphQLNonNullType()]
        public string? Scalar { get; }

        [GraphQLNonNullType]
        public string?[]? ScalarArray { get; }

        [GraphQLNonNullType]
        public string?[]?[]? ScalarNestedArray { get; }

        [GraphQLNonNullType]
        public Foo? Object { get; }

        [GraphQLNonNullType]
        public Foo?[]? ObjectArray { get; }

        [GraphQLNonNullType]
        public Foo?[]?[]? ObjectNestedArray { get; }
    }

    public class Foo
    {
        public string Bar { get; } = default!;
    }
}
