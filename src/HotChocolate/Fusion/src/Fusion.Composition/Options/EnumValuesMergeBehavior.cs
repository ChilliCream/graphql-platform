namespace HotChocolate.Fusion.Options;

/// <summary>
/// Defines how enum values are merged when the same enum type is defined in multiple source
/// schemas.
/// </summary>
public enum EnumValuesMergeBehavior
{
    /// <summary>
    /// Applies <see cref="Strict"/>, unless at least one Apollo Federation connector source
    /// schema is part of the composition, in which case <see cref="Union"/> applies.
    /// </summary>
    Auto = 0,

    /// <summary>
    /// Enum types with the same name must define identical value sets.
    /// </summary>
    Strict = 1,

    /// <summary>
    /// Values of enums used only in output positions are merged by union. Enums used in any
    /// input position still require identical value sets.
    /// </summary>
    Union = 2
}
