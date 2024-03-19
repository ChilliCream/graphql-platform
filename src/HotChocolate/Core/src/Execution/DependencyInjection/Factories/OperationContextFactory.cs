using System;
using HotChocolate.Execution.DependencyInjection;
using HotChocolate.Execution.Processing;
using HotChocolate.Execution.Processing.Tasks;
using HotChocolate.Resolvers;
using HotChocolate.Utilities;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// The <see cref="OperationContextFactory"/> creates new instances of
/// <see cref="OperationContext"/>.
///
/// Operation context lifetime is managed by the OperationContext pool and
/// the execution pipeline.
///
/// The lifetime MUST NOT be managed or tracked by the DI container.
///
/// The <see cref="OperationContextFactory"/> MUST be a singleton.
/// </summary>
internal sealed class OperationContextFactory(
    IFactory<ResolverTask> resolverTaskFactory,
    ResultPool resultPool,
    ITypeConverter typeConverter,
    AggregateServiceScopeInitializer serviceScopeInitializer)
    : IFactory<OperationContext>
{
    public OperationContext Create()
        => new OperationContext(
            resolverTaskFactory,
            new ResultBuilder(resultPool),
            typeConverter,
            serviceScopeInitializer);
}
