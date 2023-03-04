namespace HotChocolate.Skimmed;

public interface IField : IHasName, IHasDirectives, IHasContextData
{
    /// <summary>
    /// Gets the description of the field.
    /// </summary>
    string? Description { get; set; }

    /// <summary>
    /// Defines if this field is deprecated.
    /// </summary>
    bool IsDeprecated { get; set; }

    /// <summary>
    /// Gets the deprecation reason.
    /// </summary>
    string? DeprecationReason { get; set; }

    IType Type { get; set; }
}
