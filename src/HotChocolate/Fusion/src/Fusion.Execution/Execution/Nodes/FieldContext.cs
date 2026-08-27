using HotChocolate.Buffers;
using HotChocolate.Execution;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution.Nodes;

internal abstract class FieldContext
{
    public abstract PooledArrayWriter Memory { get; }
    public abstract ISchemaDefinition Schema { get; }
    public abstract Selection Selection { get; }
    public abstract SourceResultElementBuilder FieldResult { get; }
    public abstract ConditionFlags IncludeConditionFlags { get; }

    public ulong IncludeFlags => IncludeConditionFlags.Word0;

    public abstract T Parent<T>();
    public abstract T ArgumentValue<T>(string name) where T : IValueNode;
    public abstract CancellationToken RequestAborted { get; }
    public abstract void AddRuntimeResult<T>(T result);
}
