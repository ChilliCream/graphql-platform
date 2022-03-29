using System;
using System.Collections;
using System.Collections.Generic;
using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.AspNetCore.Authorization;

public class AuthorizationAttributeTestData : IEnumerable<object[]>
{
    public class Query
    {
        [Authorize]
        public string GetDefault() => "foo";

        [Authorize(Policy = "graphql/authz/has_age_defined/allow")]
        public string GetAge() => "foo";

        [Authorize(Roles = new[] { "a" })]
        public string GetRoles() => "foo";

        [Authorize(Roles = new[] { "a", "b" })]
        [GraphQLName("roles_ab")]
        public string GetRolesAb() => "foo";

        [Authorize(Policy = "a")]
        [Authorize(Policy = "b")]
        public string GetPiped() => "foo";

        [Authorize(Policy = "a", Apply = ApplyPolicy.AfterResolver)]
        public string GetAfterResolver() => "foo";
    }

    private Action<IRequestExecutorBuilder> CreateSchema() =>
        builder => builder
            .AddQueryType<Query>()
            .AddAuthorization()
            .AddOpaAuthorizationHandler((c, o) =>
            {
                o.ConnectionTimeout = TimeSpan.FromSeconds(60);
            }).AddOpaResponseHandler<HasAgeDefinedResponse>("graphql/authz/has_age_defined",
                context =>
                {
                    return context.Result switch
                    {
                        { Allow: true } => new QueryResponse<bool?> { Result = true },
                        _ => new QueryResponse<bool?> { Result = false }
                    };
                });

    public IEnumerator<object[]> GetEnumerator()
    {
        yield return new object[] { CreateSchema() };
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
