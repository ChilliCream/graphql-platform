using HotChocolate.Fusion.Language;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution.Nodes;

public record OperationRequirement(
    string Key,
    ITypeNode Type,
    SelectionPath Path,
    IValueSelectionNode Map);
