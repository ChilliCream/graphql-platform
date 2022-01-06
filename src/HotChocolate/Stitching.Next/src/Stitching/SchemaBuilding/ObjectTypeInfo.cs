using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Stitching.SchemaBuilding;


public class SchemaInfo
{
    public IList<ObjectTypeInfo> Types { get; } =
        new List<ObjectTypeInfo>();
}

public class ObjectTypeInfo
{
    public ObjectTypeInfo(NameString name, ObjectTypeDefinitionNode definition)
    {
        Name = name;
        Definition = definition;
    }

    public NameString Name { get; }

    public ObjectTypeDefinitionNode Definition { get; }

    public IList<ObjectFetcher> Fetchers { get; } =
        new List<ObjectFetcher>();
}

public readonly struct ObjectFetcher
{
    public string Source { get; }

    public ISyntaxNode Selections { get; }

    /// <summary>
    /// T
    /// </summary>
    /// <value></value>
    public IReadOnlyList<string> Fields { get; }

    public object Aggregator { get; }
}

public readonly struct FieldOrSelection
{
    public FieldOrSelection(FieldNode field)
    {
        Kind = FieldOrSelectionKind.Field;
        Field = field;
        Selection = null;
    }

    public FieldOrSelection(SelectionSetNode selection)
    {
        Kind = FieldOrSelectionKind.Selection;
        Field = null;
        Selection = selection;
    }

    public FieldOrSelectionKind Kind { get; }

    public FieldNode? Field { get; }

    public SelectionSetNode? Selection { get; }
}

public enum FieldOrSelectionKind
{
    Field,
    Selection
}