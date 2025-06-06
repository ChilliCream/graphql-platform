using System.Collections;
using HotChocolate.Authorization;
using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.AspNetCore.Authorization;

public class AuthorizationAttributeTestData : IEnumerable<object[]>
{
    public class Query
    {
        [Authorize]
        public string GetDefault() => "foo";

        [Authorize(Policy = Policies.HasDefinedAge)]
        public string? GetAge() => "foo";

        [Authorize(Roles = ["a"])]
        public string GetRoles() => "foo";

        [Authorize(Roles = ["a", "b"])]
        [GraphQLName("roles_ab")]
        public string GetRolesAb() => "foo";

        [Authorize(Policy = "a")]
        [Authorize(Policy = "b")]
        public string GetPiped() => "foo";

        [Authorize(Policy = "a", Apply = ApplyPolicy.AfterResolver)]
        public string GetAfterResolver() => "foo";
    }

    private Action<IRequestExecutorBuilder, int> CreateSchema() =>
        (builder, port) => builder
            .AddQueryType<Query>()
            .AddOpaAuthorization(
                (_, o) =>
                {
                    o.BaseAddress = new Uri($"http://127.0.0.1:{port}/v1/data/");
                    o.Timeout = TimeSpan.FromMilliseconds(60000);
                })
            .AddOpaResultHandler(
                Policies.HasDefinedAge,
                response => response.DecisionId is null
                    ? AuthorizeResult.NotAllowed
                    : response.GetResult<HasAgeDefinedResponse>() switch
                    {
                        { Allow: true } => AuthorizeResult.Allowed,
                        _ => AuthorizeResult.NotAllowed
                    });

    public IEnumerator<object[]> GetEnumerator()
    {
        yield return
        [
            CreateSchema()
        ];
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
