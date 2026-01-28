namespace HotChocolate;

/// <summary>
/// Controls whether XML documentation comments are considered when inferring GraphQL descriptions.
/// </summary>
/// <remarks>
/// By default, Hot Chocolate infers GraphQL descriptions from XML documentation comments.
/// Applying this attribute allows explicitly opting out of that behavior.
/// When applied at the assembly or type level, the setting applies to all contained members
/// unless overridden by a more specific declaration.
/// This attribute can also be used to explicitly opt back in by setting <see cref="Ignore"/> to <c>false</c>.
/// </remarks>
[AttributeUsage(
    AttributeTargets.Class
    | AttributeTargets.Struct
    | AttributeTargets.Interface
    | AttributeTargets.Method
    | AttributeTargets.Property
    | AttributeTargets.Parameter
    | AttributeTargets.Field
    | AttributeTargets.Assembly)]
public sealed class GraphQLIgnoreXmlDocumentationAttribute : Attribute
{
    /// <summary>
    /// Gets or sets whether the XML documentation should be ignored for the annotated symbol.
    /// </summary>
    /// <remarks>
    /// The default value is <c>true</c>, which disables XML documentation inference.
    /// Set this value to <c>false</c> to explicitly enable XML documentation inference
    /// when it has been disabled within the outer scope.
    /// </remarks>
    public bool Ignore { get; init; } = true;
}
