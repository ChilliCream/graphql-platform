using System.Collections;
using HotChocolate.Authorization;
using HotChocolate.Execution.Configuration;
using HotChocolate.Resolvers;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.AspNetCore.Authorization;

public class AuthorizationTestData : IEnumerable<object[]>
{
    private const string Sdl = $@"
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
        }}";

    private readonly FieldMiddleware _schemaMiddleware = next => context =>
    {
        context.Result = "foo";
        return next.Invoke(context);
    };

    private Action<IRequestExecutorBuilder, int> CreateSchema() =>
        (builder, port) => builder
            .AddDocumentFromString(Sdl)
            .AddOpaAuthorization(
                (_, o) =>
                {
                    o.BaseAddress = new Uri($"http://127.0.0.1:{port}/v1/data/");
                    o.Timeout = TimeSpan.FromMilliseconds(60000);
                })
            .AddOpaResultHandler(
                Policies.HasDefinedAge,
                response => response.DecisionId is null
                    ? AuthorizeResult.NotAllowed
                    : response.GetResult<HasAgeDefinedResponse>() switch
                    {
                        { Allow: true, } => AuthorizeResult.Allowed,
                        _ => AuthorizeResult.NotAllowed,
                    })
            .UseField(_schemaMiddleware);

    private Action<IRequestExecutorBuilder, int> CreateSchemaWithBuilder() =>
        (builder, port) => builder
            .AddDocumentFromString(Sdl)
            .AddOpaAuthorization(
                (_, o) =>
                {
                    o.BaseAddress = new Uri($"http://127.0.0.1:{port}/v1/data/");
                    o.Timeout = TimeSpan.FromMilliseconds(60000);
                })
            .AddOpaResultHandler(
                Policies.HasDefinedAge,
                response => response.DecisionId is null
                    ? AuthorizeResult.NotAllowed
                    : response.GetResult<HasAgeDefinedResponse>() switch
                    {
                        { Allow: true, } => AuthorizeResult.Allowed,
                        _ => AuthorizeResult.NotAllowed,
                    })
            .UseField(_schemaMiddleware);

    public IEnumerator<object[]> GetEnumerator()
    {
        yield return [CreateSchema(),];
        yield return [CreateSchemaWithBuilder(),];
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
