using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Stitching.Schemas.Customers;
using HotChocolate.Tests;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Stitching.Schemas
{
    public class SchemaTests
    {
        [Fact]
        public async Task CustomerSchemaSnapshot()
        {
            Snapshot.FullName();

            await new ServiceCollection()
                .AddGraphQL()
                .AddCustomerSchema()
                .BuildSchemaAsync()
                .MatchSnapshotAsync();
        }
    }
}
