using HotChocolate.Language;

namespace HotChocolate.Stitching.SchemaBuilding;

internal readonly struct FieldOrSelection
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
