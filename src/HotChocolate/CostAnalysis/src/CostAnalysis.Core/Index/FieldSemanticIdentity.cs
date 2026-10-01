using HotChocolate.Types;

namespace HotChocolate.CostAnalysis;

internal readonly record struct FieldSemanticIdentity(
    IType OutputType,
    long FieldWeight,
    long ReturnTypeWeight,
    ListSizeMetadata? ListSize,
    IReadOnlyList<InputValueMetadata> Arguments);
