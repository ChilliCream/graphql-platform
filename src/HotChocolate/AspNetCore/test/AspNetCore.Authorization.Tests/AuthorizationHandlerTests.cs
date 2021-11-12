using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Resolvers;
using HotChocolate.Tests;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.AspNetCore.Authorization;

public class AuthorizationHandlerTests
{
    [InlineData(AuthorizeResult.Allowed)]
    [InlineData(AuthorizeResult.NoDefaultPolicy)]
    [InlineData(AuthorizeResult.NotAllowed)]
    [InlineData(AuthorizeResult.NotAuthenticated)]
    [InlineData(AuthorizeResult.PolicyNotFound)]
    [Theory]
    public async Task Authorize(AuthorizeResult result)
    {
        Snapshot.FullName(new SnapshotNameExtension(result));

        await new ServiceCollection()
            .AddGraphQLServer()
            .AddQueryType()
            .AddTypeExtension<QueryExtensions>()
            .AddAuthorizationHandler<CustomHandler>()
            .ExecuteRequestAsync(
                QueryRequestBuilder
                    .New()
                    .SetQuery("{ bar }")
                    .AddProperty("auth", result)
                    .Create())
            .MatchSnapshotAsync();
    }

    [Authorize]
    [ExtendObjectType(OperationTypeNames.Query)]
    public class QueryExtensions
    {
        public string Bar() => "bar";
    }

    public class CustomHandler : IAuthorizationHandler
    {
        public ValueTask<AuthorizeResult> AuthorizeAsync(
            IMiddlewareContext context,
            AuthorizeDirective directive)
        {
            return new((AuthorizeResult)context.ContextData["auth"]!);
        }
    }
}
