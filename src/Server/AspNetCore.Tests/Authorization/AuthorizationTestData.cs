using System.Collections;
using System.Collections.Generic;
using HotChocolate.Execution;
using HotChocolate.Resolvers;

namespace HotChocolate.AspNetCore.Authorization
{
    public class AuthorizationTestData : IEnumerable<object[]>
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

        private IQueryExecutor CreateExecutor()
        {
            return Schema.Create(
                SchemaCode,
                configuration =>
                {
                    configuration.RegisterAuthorizeDirectiveType();
                    configuration.Use(SchemaMiddleware);
                })
                .MakeExecutable();
        }

        private IQueryExecutor CreateExecutorWithBuilder()
        {
            return SchemaBuilder.New()
                .AddDocumentFromString(SchemaCode)
                .AddAuthorizeDirectiveType()
                .Use(SchemaMiddleware)
                .Create()
                .MakeExecutable();
        }

        public IEnumerator<object[]> GetEnumerator()
        {
            yield return new object[] { CreateExecutor() };
            yield return new object[] { CreateExecutorWithBuilder() };
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
