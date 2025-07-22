using HotChocolate.Buffers;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution.Nodes;

internal abstract class FieldContext
{
    public abstract ResultPoolSession ResultPool { get; }
    public abstract PooledArrayWriter Memory { get; }
    public abstract ISchemaDefinition Schema { get; }
    public abstract Selection Selection { get; }
    public abstract FieldResult FieldResult { get; }
    public abstract ulong IncludeFlags { get; }
    public abstract T Parent<T>();
    public abstract T ArgumentValue<T>(string name) where T : IValueNode;
    public abstract void AddRuntimeResult<T>(T result);
}
