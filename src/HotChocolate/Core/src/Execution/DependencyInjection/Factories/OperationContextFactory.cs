using HotChocolate.Execution.DependencyInjection;
using HotChocolate.Execution.Processing;
using HotChocolate.Execution.Processing.Tasks;
using HotChocolate.Resolvers;
using HotChocolate.Utilities;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// <para>
/// The <see cref="OperationContextFactory"/> creates new instances of
/// <see cref="OperationContext"/>.
/// </para>
/// <para>
/// Operation context lifetime is managed by the OperationContext pool and
/// the execution pipeline.
/// </para>
/// <para>The lifetime MUST NOT be managed or tracked by the DI container.</para>
/// <para>The <see cref="OperationContextFactory"/> MUST be a singleton.</para>
/// </summary>
internal sealed class OperationContextFactory(
    IFactory<ResolverTask> resolverTaskFactory,
    IFactory<IResultBuilder, ResultPool> resultBuilderFactory,
    ResultPool resultPool,
    ITypeConverter typeConverter,
    AggregateServiceScopeInitializer serviceScopeInitializer)
    : IFactory<OperationContext>
{
    public OperationContext Create()
        => new OperationContext(
            resolverTaskFactory,
            resultBuilderFactory.Create(resultPool),
            typeConverter,
            serviceScopeInitializer);
}
