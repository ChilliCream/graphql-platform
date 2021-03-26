using System.Security.Claims;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Tests;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.AspNetCore.Authorization
{
    public class AuthorizeSchemaTests
    {
        [Fact]
        public async Task AuthorizeOnExtension()
        {
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType()
                .AddTypeExtension<QueryExtensions>()
                .AddAuthorization()
                .ExecuteRequestAsync(
                    QueryRequestBuilder
                        .New()
                        .SetQuery("{ bar }")
                        .AddProperty(nameof(ClaimsPrincipal), new ClaimsPrincipal())
                        .Create())
                .MatchSnapshotAsync();
        }

        [Authorize]
        [ExtendObjectType(OperationTypeNames.Query)]
        public class QueryExtensions
        {
            public string Bar() => "bar";
        }
    }
}
