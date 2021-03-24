using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Tests;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

#nullable enable

namespace HotChocolate
{
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

        public class Query
        {
            public Bar GetBar() => new();
        }

        public class Bar
        {
            public string Baz { get; set; } = default!;

            [GraphQLIgnore]
            public (string X, string? Y) IgnoreThis() => default;
        }
    }
}
