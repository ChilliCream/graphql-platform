using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.Satisfiability;

internal readonly record struct WorkItem(
    MutableObjectTypeDefinition ObjectType,
    PathNode? Path);
