namespace HotChocolate.Skimmed;

public interface IField
{
    /// <summary>
    /// Gets the field name.
    /// </summary>
    string Name { get; set; }

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

    DirectiveCollection Directives { get; }


    IDictionary<string, object?> ContextData { get; }

    IType Type { get; set; }
}
