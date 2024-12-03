using HotChocolate.Fusion.Types;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning.Nodes;

public record FieldRequirement(
    OperationPlanNode Operation,
    FieldPath Path,
    string Name,
    ITypeNode Type,
    SelectionSetNode Selections);
