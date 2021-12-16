using System.Threading.Tasks;
using HotChocolate.Tests;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;
using Xunit;

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
}
