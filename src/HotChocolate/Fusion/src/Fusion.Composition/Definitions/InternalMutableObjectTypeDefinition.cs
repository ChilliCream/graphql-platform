using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.Definitions;

/// <summary>
/// A placeholder for types that are internal in all source schemas.
/// </summary>
internal sealed class InternalMutableObjectTypeDefinition(string name)
    : MutableObjectTypeDefinition(name);
