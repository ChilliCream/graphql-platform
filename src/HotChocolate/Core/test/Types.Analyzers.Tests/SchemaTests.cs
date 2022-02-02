using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Tests;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types;

public class SchemaTests
{
    [Fact]
    public async Task SchemaSnapshot()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddCustomModule()
            .BuildSchemaAsync()
            .MatchSnapshotAsync();
            
    }
}