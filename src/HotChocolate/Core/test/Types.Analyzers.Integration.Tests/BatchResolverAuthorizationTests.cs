using CookieCrumble;
using HotChocolate.Authorization;
using HotChocolate.Execution;
using HotChocolate.Resolvers;
using HotChocolate.Types.Relay;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types;

public class BatchResolverAuthorizationTests
{
    public static TheoryData<string, string, bool> Modes()
    {
        var data = new TheoryData<string, string, bool>();
        foreach (var style in new[] { "Attribute", "Generated", "Fluent" })
        {
            foreach (var field in new[] { "before", "after", "validation" })
            {
                data.Add(style, field, false);
                data.Add(style, field, true);
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(Modes))]
    public async Task Authorize_Should_EnforcePolicy_When_BatchFieldExecutes(
        string style,
        string field,
        bool allowed)
    {
        // arrange
        var probe = new BatchAuthorizationProbe(allowed);
        var executor = await CreateExecutorAsync(style, probe);

        // act
        await using var result = await executor.ExecuteAsync(
            $"{{ parents {{ value: {field} }} }}",
            cancellationToken: TestContext.Current.CancellationToken);

        // assert
        new Snapshot()
            .SetPostFix($"{field}_{allowed}")
            .Add(result, "Result")
            .Add(probe.Calls, "Authorization calls")
            .Add(probe.Batches, "Resolver batches")
            .MatchMarkdownSnapshot();
    }

    [Theory]
    [InlineData("Attribute")]
    [InlineData("Generated")]
    [InlineData("Fluent")]
    public async Task Authorize_Should_AllowAnonymous_When_ReturnTypeIsProtected(string style)
    {
        // arrange
        var probe = new BatchAuthorizationProbe(false);
        var executor = await CreateExecutorAsync(style, probe);

        // act
        await using var result = await executor.ExecuteAsync(
            "{ parents { anonymous { id } protected { id } } }",
            cancellationToken: TestContext.Current.CancellationToken);

        // assert
        new Snapshot()
            .Add(result, "Result")
            .Add(probe.Calls.Order().ToArray(), "Authorization calls")
            .Add(probe.Batches.OrderBy(t => t.Length).ToArray(), "Resolver batches")
            .MatchMarkdownSnapshot();
    }

    public static TheoryData<string, ApplyPolicy, bool, bool> NodeModes()
    {
        var data = new TheoryData<string, ApplyPolicy, bool, bool>();
        foreach (var style in new[] { "Attribute", "Generated", "Fluent" })
        {
            foreach (var apply in new[] { ApplyPolicy.BeforeResolver, ApplyPolicy.AfterResolver, ApplyPolicy.Validation })
            {
                foreach (var allowed in new[] { false, true })
                {
                    data.Add(style, apply, allowed, false);
                    data.Add(style, apply, allowed, true);
                }
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(NodeModes))]
    public async Task Authorize_Should_EnforceNodePolicy_When_NodePipelineExecutes(
        string style,
        ApplyPolicy apply,
        bool allowed,
        bool typeLevel)
    {
        // arrange
        var probe = new BatchNodeAuthorizationProbe(allowed);
        var builder = new ServiceCollection().AddSingleton(probe).AddGraphQL()
            .AddAuthorizationHandler(_ => probe)
            .AddGlobalObjectIdentification()
            .AddObjectType<BatchAuthorizationNode>(d =>
            {
                if (typeLevel)
                {
                    d.Authorize("READ", apply: apply);
                }
            });
        if (!typeLevel)
        {
            builder.ModifyAuthorizationOptions(o => o.ConfigureNodeFields = d => d.Authorize("READ", apply: apply));
        }

        switch (style)
        {
            case "Generated":
                builder.AddQueryType(BatchAuthorizationNodeQuery.Initialize);
                break;
            case "Attribute":
                builder.AddQueryType<BatchAuthorizationNodeResolvers>();
                break;
            case "Fluent":
                builder.AddQueryType(d =>
                {
                    d.Field("authorizationNodeById")
                        .ResolveBatchWith<BatchAuthorizationNodeResolvers>(t => t.GetAuthorizationNodeById(null!, null!));
                    d.Field("validationMarker").Authorize("UNSELECTED", apply: ApplyPolicy.Validation).Resolve("marker");
                });
                break;
        }

        var executor = await builder.BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);
        var first = Convert.ToBase64String("BatchAuthorizationNode:1"u8);
        var second = Convert.ToBase64String("BatchAuthorizationNode:2"u8);

        // act
        await using var node = await executor.ExecuteAsync($"{{ node(id: \"{first}\") {{ __typename }} }}",
            cancellationToken: TestContext.Current.CancellationToken);
        var nodeCalls = probe.Calls.ToArray();
        var nodeBatches = probe.Batches.ToArray();
        probe.Calls.Clear();
        probe.Batches.Clear();
        await using var nodes = await executor.ExecuteAsync($"{{ nodes(ids: [\"{first}\", \"{second}\"]) {{ __typename }} }}",
            cancellationToken: TestContext.Current.CancellationToken);

        // assert
        new Snapshot(postFix: $"{apply}_{allowed}_{typeLevel}")
            .Add(node, "Node result")
            .Add(nodeCalls, "Node authorization calls")
            .Add(nodeBatches, "Node resolver batches")
            .Add(nodes, "Nodes result")
            .Add(probe.Calls, "Nodes authorization calls")
            .Add(probe.Batches, "Nodes resolver batches")
            .MatchMarkdownSnapshot();
    }

    private static async Task<IRequestExecutor> CreateExecutorAsync(
        string style,
        BatchAuthorizationProbe probe)
    {
        var builder = new ServiceCollection()
            .AddSingleton(probe)
            .AddGraphQL()
            .AddAuthorizationHandler(_ => probe)
            .AddQueryType(d => d.Field("parents")
                .Type<ListType<ObjectType<BatchAuthorizationParent>>>()
                .Resolve(new[]
                {
                    new BatchAuthorizationParent(1),
                    new BatchAuthorizationParent(2),
                    new BatchAuthorizationParent(3)
                }));

        switch (style)
        {
            case "Generated":
                builder.AddObjectType<BatchAuthorizationParent>(BatchAuthorizationParentType.Initialize);
                break;
            case "Attribute":
                builder.AddObjectType<BatchAuthorizationParent>(d =>
                {
                    d.Field<BatchAuthorizationResolvers>(t => t.GetBefore(null!, null!));
                    d.Field<BatchAuthorizationResolvers>(t => t.GetAfter(null!, null!));
                    d.Field<BatchAuthorizationResolvers>(t => t.GetValidation(null!, null!));
                    d.Field<BatchAuthorizationResolvers>(t => t.GetAnonymous(null!, null!));
                    d.Field<BatchAuthorizationResolvers>(t => t.GetProtected(null!, null!));
                });
                break;
            case "Fluent":
                builder.AddObjectType<BatchAuthorizationParent>(d =>
                {
                    foreach (var (name, apply) in new[]
                    {
                        ("before", ApplyPolicy.BeforeResolver),
                        ("after", ApplyPolicy.AfterResolver),
                        ("validation", ApplyPolicy.Validation)
                    })
                    {
                        d.Field(name).Type<StringType>().Authorize("READ", apply: apply)
                            .ResolveBatch(contexts =>
                            {
                                probe.Batches.Add(contexts.Select(c => c.Parent<BatchAuthorizationParent>().Id).ToArray());
                                return new ValueTask<IReadOnlyList<ResolverResult>>(contexts
                                    .Select(c => ResolverResult.Ok($"secret-{c.Parent<BatchAuthorizationParent>().Id}"))
                                    .ToArray());
                            });
                    }

                    foreach (var name in new[] { "anonymous", "protected" })
                    {
                        var field = d.Field(name).Type<ObjectType<BatchAuthorizationSecret>>()
                            .ResolveBatch(contexts =>
                            {
                                probe.Batches.Add(contexts.Select(c => c.Parent<BatchAuthorizationParent>().Id).ToArray());
                                return new ValueTask<IReadOnlyList<ResolverResult>>(contexts
                                    .Select(c => ResolverResult.Ok(new BatchAuthorizationSecret(c.Parent<BatchAuthorizationParent>().Id)))
                                    .ToArray());
                            });
                        if (name == "anonymous")
                        {
                            field.AllowAnonymous();
                        }
                    }
                });
                break;
        }

        return await builder.BuildRequestExecutorAsync(
            cancellationToken: TestContext.Current.CancellationToken);
    }
}

public sealed record BatchAuthorizationParent(int Id);

public sealed record BatchAuthorizationNode(string Id);

[QueryType]
public static partial class BatchAuthorizationNodeQuery
{
    [Authorize("UNSELECTED", ApplyPolicy.Validation)]
    public static string GetValidationMarker() => "marker";

    [NodeResolver]
    [BatchResolver]
    public static IReadOnlyList<BatchAuthorizationNode?> GetAuthorizationNodeById(
        List<string> id,
        BatchNodeAuthorizationProbe probe)
        => probe.Resolve(id);
}

public sealed class BatchAuthorizationNodeResolvers
{
    [Authorize("UNSELECTED", ApplyPolicy.Validation)]
    public string GetValidationMarker() => "marker";

    [NodeResolver]
    [BatchResolver]
    public IReadOnlyList<BatchAuthorizationNode?> GetAuthorizationNodeById(
        List<string> id,
        BatchNodeAuthorizationProbe probe)
        => probe.Resolve(id);
}

public sealed class BatchNodeAuthorizationProbe(bool allowed) : IAuthorizationHandler
{
    public List<string> Calls { get; } = [];

    public List<string[]> Batches { get; } = [];

    public IReadOnlyList<BatchAuthorizationNode?> Resolve(List<string> ids)
    {
        Batches.Add(ids.ToArray());
        return ids.Select(id => new BatchAuthorizationNode(id)).ToArray();
    }

    public ValueTask<AuthorizeResult> AuthorizeAsync(
        IMiddlewareContext context,
        AuthorizeDirective directive,
        CancellationToken cancellationToken = default)
    {
        Calls.Add($"{directive.Apply}:{context.Path}");
        return new(allowed ? AuthorizeResult.Allowed : AuthorizeResult.NotAllowed);
    }

    public ValueTask<AuthorizeResult> AuthorizeAsync(
        AuthorizationContext context,
        IReadOnlyList<AuthorizeDirective> directives,
        CancellationToken cancellationToken = default)
    {
        Calls.Add("Validation");
        return new(allowed ? AuthorizeResult.Allowed : AuthorizeResult.NotAllowed);
    }
}

[Authorize("READ")]
public sealed record BatchAuthorizationSecret(int Id);

[ObjectType<BatchAuthorizationParent>]
public static partial class BatchAuthorizationParentType
{
    [BatchResolver]
    [Authorize("READ")]
    public static IReadOnlyList<string?> GetBefore(
        [Parent] List<BatchAuthorizationParent> parents,
        BatchAuthorizationProbe probe)
        => probe.Resolve(parents);

    [BatchResolver]
    [Authorize("READ", ApplyPolicy.AfterResolver)]
    public static IReadOnlyList<string?> GetAfter(
        [Parent] List<BatchAuthorizationParent> parents,
        BatchAuthorizationProbe probe)
        => probe.Resolve(parents);

    [BatchResolver]
    [Authorize("READ", ApplyPolicy.Validation)]
    public static IReadOnlyList<string?> GetValidation(
        [Parent] List<BatchAuthorizationParent> parents,
        BatchAuthorizationProbe probe)
        => probe.Resolve(parents);

    [BatchResolver]
    [AllowAnonymous]
    public static IReadOnlyList<BatchAuthorizationSecret?> GetAnonymous(
        [Parent] List<BatchAuthorizationParent> parents,
        BatchAuthorizationProbe probe)
        => probe.ResolveSecrets(parents);

    [BatchResolver]
    public static IReadOnlyList<BatchAuthorizationSecret?> GetProtected(
        [Parent] List<BatchAuthorizationParent> parents,
        BatchAuthorizationProbe probe)
        => probe.ResolveSecrets(parents);
}

public sealed class BatchAuthorizationResolvers
{
    [BatchResolver]
    [Authorize("READ")]
    public IReadOnlyList<string?> GetBefore([Parent] List<BatchAuthorizationParent> parents, BatchAuthorizationProbe probe)
        => probe.Resolve(parents);

    [BatchResolver]
    [Authorize("READ", ApplyPolicy.AfterResolver)]
    public IReadOnlyList<string?> GetAfter([Parent] List<BatchAuthorizationParent> parents, BatchAuthorizationProbe probe)
        => probe.Resolve(parents);

    [BatchResolver]
    [Authorize("READ", ApplyPolicy.Validation)]
    public IReadOnlyList<string?> GetValidation([Parent] List<BatchAuthorizationParent> parents, BatchAuthorizationProbe probe)
        => probe.Resolve(parents);

    [BatchResolver]
    [AllowAnonymous]
    public IReadOnlyList<BatchAuthorizationSecret?> GetAnonymous([Parent] List<BatchAuthorizationParent> parents, BatchAuthorizationProbe probe)
        => probe.ResolveSecrets(parents);

    [BatchResolver]
    public IReadOnlyList<BatchAuthorizationSecret?> GetProtected([Parent] List<BatchAuthorizationParent> parents, BatchAuthorizationProbe probe)
        => probe.ResolveSecrets(parents);
}

public sealed class BatchAuthorizationProbe(bool allowed) : IAuthorizationHandler
{
    public List<string> Calls { get; } = [];

    public List<int[]> Batches { get; } = [];

    public IReadOnlyList<string?> Resolve(List<BatchAuthorizationParent> parents)
    {
        Batches.Add(parents.Select(t => t.Id).ToArray());
        return parents.Select(t => $"secret-{t.Id}").ToArray();
    }

    public IReadOnlyList<BatchAuthorizationSecret?> ResolveSecrets(List<BatchAuthorizationParent> parents)
    {
        Batches.Add(parents.Select(t => t.Id).ToArray());
        return parents.Select(t => new BatchAuthorizationSecret(t.Id)).ToArray();
    }

    public ValueTask<AuthorizeResult> AuthorizeAsync(
        IMiddlewareContext context,
        AuthorizeDirective directive,
        CancellationToken cancellationToken = default)
    {
        var id = context.Parent<BatchAuthorizationParent>().Id;
        Calls.Add($"{directive.Apply}:{context.Path}:{context.Result ?? "<null>"}");
        return new(allowed || id != 2 ? AuthorizeResult.Allowed : AuthorizeResult.NotAllowed);
    }

    public ValueTask<AuthorizeResult> AuthorizeAsync(
        AuthorizationContext context,
        IReadOnlyList<AuthorizeDirective> directives,
        CancellationToken cancellationToken = default)
    {
        Calls.Add("Validation");
        return new(allowed ? AuthorizeResult.Allowed : AuthorizeResult.NotAllowed);
    }
}
