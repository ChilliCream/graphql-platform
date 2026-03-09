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
    public abstract T Parent<T>();
    public abstract T ArgumentValue<T>(string name) where T : IValueNode;
    public abstract void AddRuntimeResult<T>(T result);
}
