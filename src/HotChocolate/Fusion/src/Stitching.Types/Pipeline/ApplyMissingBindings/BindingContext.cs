using HotChocolate.Language.Visitors;

namespace HotChocolate.Stitching.Types.Pipeline.ApplyMissingBindings;

internal sealed class BindingContext : ISyntaxVisitorContext
{
    public BindingContext(string schemaName)
    {
        SchemaName = schemaName;
    }

    public string SchemaName { get; }
}
