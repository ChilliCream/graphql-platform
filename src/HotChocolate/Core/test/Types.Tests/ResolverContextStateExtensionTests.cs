using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Tests;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Types;
using Xunit;

namespace HotChocolate
{
    public class ResolverContextStateExtensionTests
    {
        [Fact]
        public async Task GetUserClaims()
        {
            var user = new ClaimsPrincipal(
                new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, "abc")
                }));

            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType(d =>
                {
                    d.Name("Query");
                    d.Field("foo").Resolve(ctx => ctx.GetUser()?.Identity?.Name);
                })
                .ExecuteRequestAsync(
                    QueryRequestBuilder.New()
                        .SetQuery("{ foo }")
                        .SetProperty(nameof(ClaimsPrincipal), user)
                        .Create())
                .MatchSnapshotAsync();
        }
    }
}
