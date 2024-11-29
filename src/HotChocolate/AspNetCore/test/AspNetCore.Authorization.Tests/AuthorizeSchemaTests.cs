using System.Security.Claims;
using HotChocolate.Authorization;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.AspNetCore.Authorization;

public class AuthorizeSchemaTests
{
    [Fact]
    public async Task AuthorizeOnExtension()
    {
        var result = await new ServiceCollection()
            .AddLogging()
            .AddAuthorizationCore()
            .AddGraphQLServer()
            .AddQueryType()
            .AddTypeExtension<QueryExtensions>()
            .AddAuthorization()
            .ExecuteRequestAsync(
                OperationRequestBuilder
                    .New()
                    .SetDocument("{ bar }")
                    .SetUser(new ClaimsPrincipal())
                    .Build());

        result.MatchSnapshot();
    }

    [Authorize(ApplyPolicy.BeforeResolver)]
    [ExtendObjectType(OperationTypeNames.Query)]
    public class QueryExtensions
    {
        public string Bar() => "bar";
    }
}
