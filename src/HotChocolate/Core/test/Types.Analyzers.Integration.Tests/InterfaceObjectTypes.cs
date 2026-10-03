using HotChocolate.Types.Composite;

namespace HotChocolate.Types;

/// <summary>
/// The entity that <see cref="ProgrammeType"/> and the non-generic <see cref="Programme2"/>
/// stand in for.
/// </summary>
[EntityKey("id")]
public sealed class Programme
{
    public required string Id { get; set; }
}

/// <summary>
/// Declares the @interfaceObject stand-in for <see cref="Programme"/> through the generic
/// <see cref="InterfaceObjectAttribute{T}"/>; no additional [ObjectType&lt;T&gt;] is required.
/// </summary>
[InterfaceObject<Programme>]
public static partial class ProgrammeType
{
    public static string[] GetAllowedUserActions([Parent] Programme programme)
        => ["view", "edit"];
}

/// <summary>
/// Declares the @interfaceObject stand-in through the non-generic <see cref="InterfaceObjectAttribute"/>
/// applied directly to the runtime class, with no [ObjectType] attribute alongside it. This
/// relies solely on the generated module's auto-registration.
/// </summary>
[InterfaceObject]
[EntityKey("id")]
public sealed class Programme2
{
    public required string Id { get; set; }
}

// Contributes the covering lookup for Programme to the shared Query type through a
// by-name object type extension, kept separate from the generator-owned Query root partials.
[ExtendObjectType(OperationTypeNames.Query)]
public sealed class ProgrammeLookupType
{
    [Lookup]
    [Internal]
    public Programme? GetProgrammeById([Is("id")] string id)
        => id == "1" ? new Programme { Id = id } : null;
}
