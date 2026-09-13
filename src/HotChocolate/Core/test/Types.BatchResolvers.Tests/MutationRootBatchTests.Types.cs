using HotChocolate.Execution.Configuration;
using HotChocolate.Resolvers;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types.BatchResolvers;

public sealed partial class MutationRootBatchTests
{
    /// <summary>
    /// HC0137 (ErrorCodes.Analyzers.BatchResolverOnMutationField) is a compile-time
    /// analyzer diagnostic raised for a single-declaration source-generated type: a
    /// <c>[MutationType]</c> static partial class whose own method carries
    /// <c>[BatchResolver]</c>. Declaring that shape here would fail the whole assembly's build,
    /// so the real diagnostic proof lives in
    /// BatchResolverOnMutationFieldAnalyzerTests.BatchResolver_Should_RaiseError_When_HostedOnMutationTypeClass
    /// (Types.Analyzers.Tests/__snapshots__).
    /// </summary>
    private const string SourceGeneratedNotApplicableReason =
        "compile-time diagnostic HC0137; see BatchResolverOnMutationFieldAnalyzerTests."
        + "BatchResolver_Should_RaiseError_When_HostedOnMutationTypeClass";

    private void ConfigureAttribute(IRequestExecutorBuilder builder)
        => builder
            .AddQueryType(d => d.Name("Query").Field("noop").Resolve("noop"))
            .AddMutationType<MutationRootAttributeMutation>(d => d.Name("Mutation"));

    private void ConfigureFluent(IRequestExecutorBuilder builder)
        => builder
            .AddQueryType(d => d.Name("Query").Field("noop").Resolve("noop"))
            .AddMutationType(d =>
            {
                d.Name("Mutation");
                d.Field("appendLog")
                    .Argument("s", a => a.Type<NonNullType<StringType>>())
                    .Type<StringType>()
                    .ResolveBatch(contexts =>
                    {
                        var results = new ResolverResult[contexts.Count];

                        for (var i = 0; i < contexts.Count; i++)
                        {
                            results[i] = ResolverResult.Ok(contexts[i].ArgumentValue<string>("s"));
                        }

                        return new ValueTask<IReadOnlyList<ResolverResult>>(results);
                    });
            });
}

/// <summary>
/// Attribute-style mutation type whose single field is a batch resolver, which must fail schema
/// build with HC0135 naming the declaring member (hc-0-6cq.5).
/// </summary>
public sealed class MutationRootAttributeMutation
{
    [BatchResolver]
    public List<string> AppendLog(List<string> s) => s;
}
