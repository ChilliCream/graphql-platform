using System.Collections.Immutable;
using HotChocolate.Buffers;
using HotChocolate.Features;

namespace HotChocolate.Execution;

/// <summary>
/// Represents the GraphQL request context.
/// </summary>
public abstract class RequestContext : IFeatureProvider
{
    private MemoryArena? _memory;

    /// <summary>
    /// Gets the GraphQL schema definition against which the request is executed.
    /// </summary>
    public abstract ISchemaDefinition Schema { get; }

    /// <summary>
    /// Gets the request executor version.
    /// </summary>
    public abstract ulong ExecutorVersion { get; }

    /// <summary>
    /// Gets the GraphQL request definition.
    /// </summary>
    public abstract IOperationRequest Request { get; }

    /// <summary>
    /// Gets the GraphQL request service provider.
    /// </summary>
    public abstract IServiceProvider RequestServices { get; set; }

    /// <summary>
    /// Gets information about the GraphQL operation document of the GraphQL <see cref="Request"/>.
    /// </summary>
    public abstract OperationDocumentInfo OperationDocumentInfo { get; }

    /// <summary>
    /// Gets the request cancellation token.
    /// </summary>
    public CancellationToken RequestAborted { get; set; }

    /// <summary>
    /// Gets or sets the request index.
    /// </summary>
    public abstract int RequestIndex { get; }

    /// <summary>
    /// Gets or sets the variable values set.
    /// </summary>
    public ImmutableArray<IVariableValueCollection> VariableValues { get; set; } = [];

    /// <summary>
    /// Gets or sets GraphQL execution result.
    /// </summary>
    public IExecutionResult? Result { get; set; }

    /// <summary>
    /// Gets the feature collection.
    /// </summary>
    public abstract IFeatureCollection Features { get; }

    /// <summary>
    /// Gets the request context data which allows arbitrary data to be stored on the request context.
    /// </summary>
    public abstract IDictionary<string, object?> ContextData { get; }

    /// <summary>
    /// Gets the memory arena that backs request-scoped allocations, or <c>null</c> when none is attached.
    /// </summary>
    internal MemoryArena? Memory => _memory;

    /// <summary>
    /// Attaches a memory arena to this request. The arena is owned by the request until it is
    /// detached on success or disposed when the context is reset.
    /// </summary>
    internal void AttachMemory(MemoryArena memory)
    {
        if (_memory is not null)
        {
            throw new InvalidOperationException("A memory arena is already attached to this request.");
        }

        _memory = memory;
    }

    /// <summary>
    /// Detaches the memory arena so its ownership can be transferred to the execution result.
    /// </summary>
    internal MemoryArena? TryDetachMemory() => Interlocked.Exchange(ref _memory, null);
}
