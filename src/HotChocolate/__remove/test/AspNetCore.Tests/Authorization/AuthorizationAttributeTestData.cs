using System.Collections;
using System.Collections.Generic;

namespace HotChocolate.AspNetCore.Authorization
{
    public class AuthorizationAttributeTestData
        : IEnumerable<object[]>
    {
        public class Query
        {
            [Authorize]
            public string GetDefault() => "foo";

            [Authorize(Policy = "HasAgeDefined")]
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

        private ISchema CreateSchema()
        {
            return SchemaBuilder.New()
                .AddQueryType<Query>()
                .AddAuthorizeDirectiveType()
                .Create();
        }

        public IEnumerator<object[]> GetEnumerator()
        {
            yield return new object[] { CreateSchema() };
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
