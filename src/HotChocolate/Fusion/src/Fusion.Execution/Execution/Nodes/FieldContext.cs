using HotChocolate.Buffers;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution.Nodes;

internal abstract class FieldContext
{
    public abstract PooledArrayWriter Memory { get; }
    public abstract ISchemaDefinition Schema { get; }
    public abstract Selection Selection { get; }
    public abstract SourceResultElementBuilder FieldResult { get; }
    public abstract ulong IncludeFlags { get; }

    /// <summary>
    /// Gets the include flag overflow words (condition indexes 64 and above) for the
    /// current request; empty if the operation has at most 64 include conditions.
    /// </summary>
    public abstract ReadOnlySpan<ulong> WideIncludeFlags { get; }

    public abstract T Parent<T>();
    public abstract T ArgumentValue<T>(string name) where T : IValueNode;
    public abstract CancellationToken RequestAborted { get; }
    public abstract void AddRuntimeResult<T>(T result);
}
