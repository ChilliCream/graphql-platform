using HotChocolate.Fusion.Types;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

public record OperationRequirement(
    string Key,
    ITypeNode Type,
    SelectionPath Path,
    FieldPath Map);
