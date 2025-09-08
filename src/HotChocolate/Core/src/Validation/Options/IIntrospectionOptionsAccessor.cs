namespace HotChocolate.Validation.Options;

/// <summary>
/// Represents the options for introspection rules.
/// </summary>
public interface IIntrospectionOptionsAccessor
{
    /// <summary>
    /// Defines if introspection is disabled.
    /// </summary>
    bool DisableIntrospection { get; }

    /// <summary>
    /// Defines if the introspection depth rule is disabled.
    /// </summary>
    bool DisableDepthRule { get; }

    /// <summary>
    /// Specifies the maximum allowed `ofType` field depth
    /// when running the introspection depth rule.
    /// </summary>
    ushort MaxAllowedOfTypeDepth { get; }

    /// <summary>
    /// Specifies the maximum allowed list recursive depth
    /// when running the introspection depth rule.
    /// </summary>
    ushort MaxAllowedListRecursiveDepth { get; }
}
