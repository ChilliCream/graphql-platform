using System.Collections;
using System.Collections.Generic;
using HotChocolate.Resolvers;

namespace HotChocolate.AspNetCore.Authorization
{
    public class AuthorizationTestData
        : IEnumerable<object[]>
    {
        private readonly string SchemaCode = @"
            type Query {
                default: String @authorize
                age: String @authorize(policy: ""HasAgeDefined"")
                roles: String @authorize(roles: [""a""])
                roles_ab: String @authorize(roles: [""a"" ""b""])
                piped: String
                    @authorize(policy: ""a"")
                    @authorize(policy: ""b"")
            }
        ";

        private FieldMiddleware SchemaMiddleware = next => context =>
        {
            context.Result = "foo";
            return next.Invoke(context);
        };

        private ISchema CreateSchema()
        {
            return Schema.Create(
                SchemaCode,
                configuration =>
                {
                    configuration.RegisterAuthorizeDirectiveType();
                    configuration.Use(SchemaMiddleware);
                });
        }

        private ISchema CreateSchemaWithBuilder()
        {
            return SchemaBuilder.New()
                .AddDocumentFromString(SchemaCode)
                .AddAuthorizeDirectiveType()
                .Use(SchemaMiddleware)
                .Create();
        }

        public IEnumerator<object[]> GetEnumerator()
        {
            yield return new object[] { CreateSchema() };
            yield return new object[] { CreateSchemaWithBuilder() };
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

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
