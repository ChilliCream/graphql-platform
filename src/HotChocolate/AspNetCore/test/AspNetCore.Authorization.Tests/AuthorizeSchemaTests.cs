using System.Security.Claims;
using CookieCrumble;
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
            .AddGraphQLServer()
            .AddQueryType()
            .AddTypeExtension<QueryExtensions>()
            .AddAuthorization()
            .ExecuteRequestAsync(
                QueryRequestBuilder
                    .New()
                    .SetQuery("{ bar }")
                    .AddGlobalState(nameof(ClaimsPrincipal), new ClaimsPrincipal())
                    .Create());

        result.MatchSnapshot();
    }

    [Authorize]
    [ExtendObjectType(OperationTypeNames.Query)]
    public class QueryExtensions
    {
        public string Bar() => "bar";
    }
}
