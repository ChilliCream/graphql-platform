using System;
using System.Collections;
using System.Collections.Generic;
using HotChocolate.Execution.Configuration;
using HotChocolate.Resolvers;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.AspNetCore.Authorization;

public class HasAgeDefinedResponse
{
    public bool Allow { get; set; }
    public Claims Claims { get; set; }
}

public class Claims
{
    public string Birthdate { get; set; }
    public long Iat { get; set; }
    public string Name { get; set; }
    public string Sub { get; set; }
}

public class AuthorizationTestData : IEnumerable<object[]>
{
    private readonly string SchemaCode = $@"
            type Query {{
                default: String @authorize
                age: String @authorize(policy: ""{Policies.HasDefinedAge}"")
                roles: String @authorize(roles: [""a""])
                roles_ab: String @authorize(roles: [""a"" ""b""])
                piped: String
                    @authorize(policy: ""a"")
                    @authorize(policy: ""b"")
                afterResolver: String
                    @authorize(policy: ""a"" apply: AFTER_RESOLVER)
            }}
        ";

    private readonly FieldMiddleware _schemaMiddleware = next => context =>
    {
        context.Result = "foo";
        return next.Invoke(context);
    };

    private Action<IRequestExecutorBuilder> CreateSchema() =>
        sb => sb
            .AddDocumentFromString(SchemaCode)
            .AddAuthorization()
            .AddOpaAuthorizationHandler((c, o) =>
            {
                o.TimeoutMs = 60000;
            })
            .AddOpaResultHandler<HasAgeDefinedResponse>(Policies.HasDefinedAge,
               x => x switch
               {
                   { Result.Allow: true } => x.Allowed(),
                   _ => x.NotAllowed(),
               })
            .UseField(_schemaMiddleware);

    private Action<IRequestExecutorBuilder> CreateSchemaWithBuilder() =>
        sb => sb
            .AddDocumentFromString(SchemaCode)
            .AddAuthorization()
            .AddOpaAuthorizationHandler((c, o) =>
            {
                o.TimeoutMs = 60000;
            })
            .AddOpaResultHandler<HasAgeDefinedResponse>(Policies.HasDefinedAge,
                x => x switch
                {
                    { Result.Allow: true } => x.Allowed(),
                    _ => x.NotAllowed(),
                })
            .UseField(_schemaMiddleware);

    public IEnumerator<object[]> GetEnumerator()
    {
        yield return new object[] { CreateSchema() };
        yield return new object[] { CreateSchemaWithBuilder() };
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
