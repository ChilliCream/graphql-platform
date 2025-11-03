using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

public record FieldSelection(
    uint SelectionSetId,
    FieldNode Node,
    FusionOutputFieldDefinition Field,
    SelectionPath Path);
