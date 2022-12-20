using HotChocolate.Language;

namespace HotChocolate.Stitching.Types.Bindings;

internal readonly struct FetchBinding : IBinding
{
    public FetchBinding(SchemaCoordinate target, string schemaName, SelectionSetNode selections)
    {
        Target = target;
        SchemaName = schemaName;
        Selections = selections;
    }

    public SchemaCoordinate Target { get; }

    public string SchemaName { get; }

    public SelectionSetNode Selections { get; }
}
