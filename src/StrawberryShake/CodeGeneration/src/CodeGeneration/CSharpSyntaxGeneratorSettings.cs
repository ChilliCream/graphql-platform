namespace StrawberryShake.CodeGeneration;

/// <summary>
/// Settings for the syntax generation.
/// </summary>
public class CSharpSyntaxGeneratorSettings
{
    /// <summary>
    /// Creates a new code generator settings instance.
    /// </summary>
    public CSharpSyntaxGeneratorSettings(
        AccessModifier accessModifier,
        bool noStore,
        bool inputRecords,
        bool entityRecords,
        bool razorComponents)
    {
        AccessModifier = accessModifier;
        NoStore = noStore;
        InputRecords = inputRecords;
        EntityRecords = entityRecords;
        RazorComponents = razorComponents;
    }

    /// <summary>
    /// Generates the client with specified access modifier.
    /// </summary>
    public AccessModifier AccessModifier { get; }

    /// <summary>
    /// Generates the client without a store
    /// </summary>
    public bool NoStore { get; }

    /// <summary>
    /// Generates input types as records.
    /// </summary>
    public bool InputRecords { get; }

    /// <summary>
    /// Generates entities as records.
    /// </summary>
    public bool EntityRecords { get; }

    /// <summary>
    /// Generate Razor components.
    /// </summary>
    public bool RazorComponents { get; }
}
