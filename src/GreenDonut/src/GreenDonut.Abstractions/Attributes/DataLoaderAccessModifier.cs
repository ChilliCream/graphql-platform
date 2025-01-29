namespace GreenDonut;

/// <summary>
/// Specifies the access modifiers for generated DataLoader.
/// </summary>
public enum DataLoaderAccessModifier
{
    /// <summary>
    /// Uses the default access modifier settings.
    /// </summary>
    Default = 0,

    /// <summary>
    /// Generates a public DataLoader class and a public DataLoader interface.
    /// </summary>
    Public = 1,

    /// <summary>
    /// Generates an internal DataLoader class and a public DataLoader interface.
    /// </summary>
    PublicInterface = 2,

    /// <summary>
    /// Generates an internal DataLoader class and an internal DataLoader interface.
    /// </summary>
    Internal = 3,
}
