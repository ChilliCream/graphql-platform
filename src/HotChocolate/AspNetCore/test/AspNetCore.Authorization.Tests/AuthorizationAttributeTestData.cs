using System.Collections;
using HotChocolate.Authorization;
using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.AspNetCore.Authorization;

public class AuthorizationAttributeTestData : IEnumerable<object[]>
{
    public class Query
    {
        [Authorize(ApplyPolicy.BeforeResolver)]
        public string? GetDefault() => "foo";

        [Authorize("HasAgeDefined", ApplyPolicy.BeforeResolver)]
        public string? GetAge() => "foo";

        [Authorize(ApplyPolicy.BeforeResolver, Roles = ["a",])]
        public string? GetRoles() => "foo";

        [Authorize(ApplyPolicy.BeforeResolver, Roles = ["a", "b",])]
        [GraphQLName("roles_ab")]
        public string? GetRolesAb() => "foo";

        [Authorize(ApplyPolicy.BeforeResolver, Roles = ["a", "b"], Policy = "HasAgeDefined")]
        [GraphQLName("rolesAndPolicy")]
        public string? GetRolesAndPolicy() => "foo";

        [Authorize(ApplyPolicy.BeforeResolver, Policy = "a")]
        [Authorize(ApplyPolicy.BeforeResolver, Policy = "b")]
        public string? GetPiped() => "foo";

        [Authorize(Policy = "a", Apply = ApplyPolicy.AfterResolver)]
        public string? GetAfterResolver() => "foo";
    }

    private Action<IRequestExecutorBuilder> CreateSchema() =>
        builder => builder
            .AddQueryType<Query>()
            .AddAuthorization();

    public IEnumerator<object[]> GetEnumerator()
    {
        yield return [CreateSchema(),];
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
