using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Tests;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;
using Xunit;

#nullable enable

namespace HotChocolate.Execution;

public class ClientControlledNullabilityTests
{
    [Fact]
    public async Task Make_NullableField_NonNull_And_Return_Null()
    {
        Snapshot.FullName();

        await new ServiceCollection()
            .AddGraphQL()
            .AddDocumentFromString("type Query { field: String }")
            .AddResolver("Query", "field", _ => null)
            .ExecuteRequestAsync("{ field! }")
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task ErrorBoundary_Bio_NonNull_Person_Nullable()
    {
        Snapshot.FullName();

        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .ExecuteRequestAsync(@"
                {
                    persons? {
                        name
                        bio!
                    }
                }")
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task ErrorBoundary_Bio_NonNull_Person_Element_Nullable()
    {
        Snapshot.FullName();

        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .ExecuteRequestAsync(@"
                {
                    persons[?] {
                        name
                        bio!
                    }
                }")
            .MatchSnapshotAsync();
    }

    public class Query
    {
        public List<Person> Persons { get; } =
        [
            new Person { Name = "Abc", Bio = "Def", },
            new Person { Name = "Ghi", Bio = null, },
        ];
    }

    public class Person
    {
        public string Name { get; set; } = default!;

        public string? Bio { get; set; }
    }
}
