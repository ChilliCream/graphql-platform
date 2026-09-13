using System.Reflection;
using HotChocolate.Execution.Configuration;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types.BatchResolvers;

public sealed partial class PerParentMiddlewareBatchTests
{
    private void ConfigureAttribute(IRequestExecutorBuilder builder)
        => builder
            .AddQueryType(d =>
            {
                d.Name("Query");
                d.Field("users")
                    .Type<ListType<ObjectType<PerParentMiddlewareUser>>>()
                    .Resolve(new List<PerParentMiddlewareUser> { new(1, "Alice"), new(2, "Bob") });
            })
            .AddTypeExtension<PerParentMiddlewareUserAttributeExtension>();

    private void ConfigureSourceGenerated(IRequestExecutorBuilder builder)
        => builder
            .AddQueryType(d =>
            {
                d.Name("Query");
                d.Field("users")
                    .Type<ListType<ObjectType<PerParentMiddlewareUser>>>()
                    .Resolve(new List<PerParentMiddlewareUser> { new(1, "Alice"), new(2, "Bob") });
            })
            .AddObjectType<PerParentMiddlewareUser>(PerParentMiddlewareUserNode.Initialize);

    private void ConfigureFluent(IRequestExecutorBuilder builder)
        => builder
            .AddQueryType(d =>
            {
                d.Name("Query");
                d.Field("users")
                    .Type<ListType<ObjectType<PerParentMiddlewareUser>>>()
                    .Resolve(new List<PerParentMiddlewareUser> { new(1, "Alice"), new(2, "Bob") });
            })
            .AddObjectType<PerParentMiddlewareUser>(d =>
            {
                d.Field(u => u.Name);
                d.Field("greeting")
                    .Type<StringType>()
                    .Use(next => async ctx =>
                    {
                        await next(ctx);
                        ctx.Result = $"wrapped({ctx.Result})";
                    })
                    .ResolveBatch(contexts =>
                    {
                        var results = new ResolverResult[contexts.Count];

                        for (var i = 0; i < contexts.Count; i++)
                        {
                            var user = contexts[i].Parent<PerParentMiddlewareUser>();
                            results[i] = ResolverResult.Ok($"Hello, {user.Name}!");
                        }

                        return new ValueTask<IReadOnlyList<ResolverResult>>(results);
                    });
            });
}

public sealed record PerParentMiddlewareUser(int Id, string Name);

/// <summary>
/// Test-local field middleware attribute that only supports the per-parent resolver pipeline
/// (calling <see cref="IObjectFieldDescriptor.Use(FieldMiddleware)"/> directly). It carries no
/// well-known middleware key, so it raises no HC0138 analyzer diagnostic under the decided fixed
/// list (unlike <c>UseDataLoader</c>/<c>UseFirstOrDefault</c>/paging/sorting/filtering): its
/// source-generated cell is a real HC0134 schema error at runtime, not a compile-time one.
/// </summary>
public sealed class UseWrapAttribute : ObjectFieldDescriptorAttribute
{
    protected override void OnConfigure(
        IDescriptorContext context,
        IObjectFieldDescriptor descriptor,
        MemberInfo? member)
        => descriptor.Use(next => next);
}

[ExtendObjectType<PerParentMiddlewareUser>]
public sealed class PerParentMiddlewareUserAttributeExtension
{
    [BatchResolver]
    [UseWrap]
    public List<string> GetGreeting([Parent] List<PerParentMiddlewareUser> users)
        => users.ConvertAll(u => $"Hello, {u.Name}!");
}

[ObjectType<PerParentMiddlewareUser>]
public static partial class PerParentMiddlewareUserNode
{
    [BatchResolver]
    [UseWrap]
    public static List<string> GetGreeting([Parent] List<PerParentMiddlewareUser> users)
        => users.ConvertAll(u => $"Hello, {u.Name}!");
}
