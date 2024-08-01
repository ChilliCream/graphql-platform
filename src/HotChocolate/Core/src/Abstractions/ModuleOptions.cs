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
}
