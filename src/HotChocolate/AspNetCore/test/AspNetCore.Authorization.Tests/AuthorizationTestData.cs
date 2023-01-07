using System.Collections;
using HotChocolate.Execution.Configuration;
using HotChocolate.Resolvers;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.AspNetCore.Authorization;

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
            afterResolver: String
                @authorize(policy: ""a"" apply: AFTER_RESOLVER)
        }";

    private readonly FieldMiddleware _schemaMiddleware = next => context =>
    {
        context.Result = "foo";
        return next.Invoke(context);
    };

    private Action<IRequestExecutorBuilder> CreateSchema() =>
        sb => sb
            .AddDocumentFromString(SchemaCode)
            .AddAuthorization()
            .UseField(_schemaMiddleware);

    private Action<IRequestExecutorBuilder> CreateSchemaWithBuilder() =>
        sb => sb
            .AddDocumentFromString(SchemaCode)
            .AddAuthorization()
            .UseField(_schemaMiddleware);

    public IEnumerator<object[]> GetEnumerator()
    {
        yield return new object[] { CreateSchema() };
        yield return new object[] { CreateSchemaWithBuilder() };
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
