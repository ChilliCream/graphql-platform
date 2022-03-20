using System;
using System.Collections;
using System.Collections.Generic;
using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MicrosoftAuthorize = Microsoft.AspNetCore.Authorization.AuthorizeAttribute;

namespace HotChocolate.AspNetCore.Authorization;

public class MicrosoftAuthorizeAttributeTestData : IEnumerable<object[]>
{
    public class Query
    {
        [MicrosoftAuthorize]
        public string GetDefault() => "foo";

        [MicrosoftAuthorize(Policy = "HasAgeDefined")]
        public string GetAge() => "foo";

        [MicrosoftAuthorize(Roles = "a")]
        public string GetRoles() => "foo";

        [MicrosoftAuthorize(Roles = "a,b")]
        [GraphQLName("roles_ab")]
        public string GetRolesAb() => "foo";

        [MicrosoftAuthorize(Policy = "a")]
        [MicrosoftAuthorize(Policy = "b")]
        public string GetPiped() => "foo";
    }

    private Action<IRequestExecutorBuilder> CreateSchema() =>
        builder => builder
            .AddQueryType<Query>()
            .AddAuthorization();

    public IEnumerator<object[]> GetEnumerator()
    {
        yield return new object[] { CreateSchema() };
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
