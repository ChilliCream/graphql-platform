using HotChocolate.Fusion.Types;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning.Nodes;

public record FieldRequirementPlanNode(
    string Name,
    OperationPlanNode From,
    FieldPath SelectionSet,
    FieldPath RequiredField,
    ITypeNode Type);
