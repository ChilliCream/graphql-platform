namespace GreenDonut;

/// <summary>
/// Specified the DataLoader source generator defaults.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly)]
public sealed class DataLoaderDefaultsAttribute : Attribute
{
    /// <summary>
    /// Specifies how services by default are handled.
    /// </summary>
    public DataLoaderServiceScope ServiceScope { get; set; }

    /// <summary>
    /// Specifies if DataLoader by default are generated as public or private class.
    /// </summary>
    public DataLoaderAccessModifier AccessModifier { get; set; }

    /// <summary>
    /// Specifies if module registration code for DataLoaders shall be generated.
    /// </summary>
    public bool GenerateRegistrationCode { get; set; } = true;

    /// <summary>
    /// Specifies if interfaces for DataLoaders shall be generated.
    /// </summary>
    public bool GenerateInterfaces { get; set; } = true;
}
