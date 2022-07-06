using System;
using System.Collections.Immutable;
using System.Threading;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Processing.Tasks;
using HotChocolate.Types;
using HotChocolate.Utilities;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Execution.Processing;


internal interface IOperationContext : IHasContextData
{
    // services / di
    IServiceProvider Services { get; }

    IActivator Activator { get; }

    // utilities
    IErrorHandler ErrorHandler { get; }

    ITypeConverter Converter { get; }

    IExecutionDiagnosticEvents DiagnosticEvents { get; }

    PathFactory PathFactory { get; }

    // request
    CancellationToken RequestAborted { get; }

    // operation
    ISchema Schema { get; }

    IOperation Operation { get; }

    IVariableValueCollection Variables { get; }

    object? RootValue { get; }

    long IncludeFlags { get; }

    ISelectionSet CollectFields(
        ISelection selection,
        IObjectType typeContext);

    T GetQueryRoot<T>();


    // Execution
    IWorkScheduler Scheduler { get; }

    IDeferredWorkScheduler DeferredScheduler { get; }

    ResultBuilder Result { get; }

    void RegisterForCleanup(Action action);

    ResolverTask CreateResolverTask(ISelection selection,
        object? parent,
        ObjectResult parentResult,
        int responseIndex,
        Path path,
        IImmutableDictionary<string, object?> scopedContextData);
}
