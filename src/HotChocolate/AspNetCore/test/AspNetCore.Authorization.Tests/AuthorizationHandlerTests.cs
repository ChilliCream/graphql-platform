using CookieCrumble;
using HotChocolate.Authorization;
using HotChocolate.Execution;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.AspNetCore.Authorization;

public class AuthorizationHandlerTests
{
    [InlineData(AuthorizeResult.Allowed)]
    [InlineData(AuthorizeResult.NoDefaultPolicy)]
    [InlineData(AuthorizeResult.NotAllowed)]
    [InlineData(AuthorizeResult.NotAuthenticated)]
    [InlineData(AuthorizeResult.PolicyNotFound)]
    [Theory]
    public async Task Authorize(AuthorizeResult authResult)
    {
        var result = await new ServiceCollection()
            .AddGraphQLServer()
            .AddQueryType()
            .AddTypeExtension<QueryExtensions>()
            .AddAuthorizationHandler<CustomHandler>()
            .ExecuteRequestAsync(
                OperationRequestBuilder
                    .Create()
                    .SetDocument("{ bar }")
                    .AddGlobalState("auth", authResult)
                    .Build());

        result.MatchSnapshot(authResult);
    }

    [ExtendObjectType(OperationTypeNames.Query)]
    public class QueryExtensions
    {
        [Authorize(ApplyPolicy.BeforeResolver)]
        public string Bar() => "bar";
    }

    public class CustomHandler : IAuthorizationHandler
    {
        public ValueTask<AuthorizeResult> AuthorizeAsync(
            IMiddlewareContext context,
            AuthorizeDirective directive,
            CancellationToken cancellationToken = default)
        {
            return new((AuthorizeResult)context.ContextData["auth"]!);
        }

        public ValueTask<AuthorizeResult> AuthorizeAsync(
            AuthorizationContext context,
            IReadOnlyList<AuthorizeDirective> directives,
            CancellationToken cancellationToken = default)
        {
            return new((AuthorizeResult)context.ContextData["auth"]!);
        }
    }
}
