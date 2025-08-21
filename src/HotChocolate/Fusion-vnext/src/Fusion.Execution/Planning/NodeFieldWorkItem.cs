using HotChocolate.Fusion.Types;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

public sealed record NodeFieldWorkItem(FieldNode NodeField) : WorkItem;

// TODO: Move
public sealed record NodeFieldLookupWorkItem(
    Lookup Lookup,
    IValueNode IdArgumentValue,
    SelectionSet SelectionSet,
    NodePlanStep Parent) : WorkItem;
