using HotChocolate.Types;

namespace HotChocolate.Fusion;

internal record MockObject(
    object? Id,
    ObjectType Type,
    int? Index = null);
