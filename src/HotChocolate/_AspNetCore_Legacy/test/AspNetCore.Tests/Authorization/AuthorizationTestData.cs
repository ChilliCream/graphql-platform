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
                afterResolver: String
                    @authorize(policy: ""a"" apply: AFTER_RESOLVER)
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
}
