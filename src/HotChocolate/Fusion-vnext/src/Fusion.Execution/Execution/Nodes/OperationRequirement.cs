using HotChocolate.Fusion.Types;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution.Nodes;

public record OperationRequirement(
    string Key,
    ITypeNode Type,
    SelectionPath Path,
    FieldPath Map);
