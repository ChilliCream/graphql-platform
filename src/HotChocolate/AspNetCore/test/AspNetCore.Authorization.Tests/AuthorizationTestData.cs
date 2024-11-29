using System.Collections;
using HotChocolate.Execution.Configuration;
using HotChocolate.Resolvers;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.AspNetCore.Authorization;

public class AuthorizationTestData : IEnumerable<object[]>
{
    private readonly string SchemaCode = @"
        type Query {
            default: String @authorize(apply: BEFORE_RESOLVER)
            age: String @authorize(policy: ""HasAgeDefined"" apply: BEFORE_RESOLVER)
            roles: String @authorize(roles: [""a""] apply: BEFORE_RESOLVER)
            roles_ab: String @authorize(roles: [""a"" ""b""] apply: BEFORE_RESOLVER)
            rolesAndPolicy: String @authorize(roles: [""a"" ""b""] policy: ""HasAgeDefined"" apply: BEFORE_RESOLVER)
            piped: String
                @authorize(policy: ""a"" apply: BEFORE_RESOLVER)
                @authorize(policy: ""b"" apply: BEFORE_RESOLVER)
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
        yield return [CreateSchema(),];
        yield return [CreateSchemaWithBuilder(),];
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
