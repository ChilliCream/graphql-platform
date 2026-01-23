namespace HotChocolate.Fusion.Options;

/// <summary>
/// Configuration options for validating the satisfiability of the composed schema.
/// </summary>
public sealed class SatisfiabilityOptions
{
    /// <summary>
    /// Gets a value indicating whether to show paths in satisfiability error messages.
    /// </summary>
    public bool IncludeSatisfiabilityPaths { get; set; }

    /// <summary>
    /// A collection of fields that should be ignored during the satisfiability analysis
    /// because they are known to be non-accessible. The key is the qualified field name (e.g.
    /// "TypeName.fieldName") and the value is a list of paths to ignore for that field.
    /// </summary>
    public Dictionary<string, List<string>> IgnoredNonAccessibleFields { get; } = [];
}
