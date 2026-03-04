namespace HotChocolate;

/// <summary>
/// The source generator module options.
/// </summary>
[Flags]
public enum ModuleOptions
{
    /// <summary>
    /// Default options.
    /// </summary>
    Default = RegisterDataLoader | RegisterTypes,

    /// <summary>
    /// Register types with the source generated module.
    /// </summary>
    RegisterTypes = 1,

    /// <summary>
    /// Register DataLoader with the source generated module.
    /// </summary>
    RegisterDataLoader = 2,

    /// <summary>
    /// Include internal resolver members when discovering source generated types.
    /// </summary>
    IncludeInternalMembers = 4,

    /// <summary>
    /// Disable XML documentation comment extraction for source generated types.
    /// When set, only explicit <see cref="GraphQLDescriptionAttribute"/> values
    /// will be used as descriptions in the generated schema.
    /// </summary>
    DisableXmlDocumentation = 8
}
