using HotChocolate.Types.Descriptors;
using static HotChocolate.ApolloFederation.ThrowHelper;

namespace HotChocolate.ApolloFederation;

/// <summary>
/// <code>
/// directive @composeDirective(name: String!) repeatable on SCHEMA
/// </code>
///
/// By default, Supergraph schema excludes all custom directives. The @composeDirective is used to specify
/// custom directives that should be exposed in the Supergraph schema.
///
/// <example>
/// extend schema @composeDirective(name: "@custom")
///   @link(url: "https://specs.apollo.dev/federation/v2.5", import: ["@composeDirective"])
///   @link(url: "https://myspecs.dev/custom/v1.0", import: ["@custom"])
///
/// directive @custom on FIELD_DEFINITION
///
/// type Query {
///   helloWorld: String! @custom
/// }
/// </example>
/// </summary>
[AttributeUsage(
    AttributeTargets.Class
    | AttributeTargets.Struct,
    Inherited = true,
    AllowMultiple = true)]
public sealed class ComposeDirectiveAttribute : SchemaTypeDescriptorAttribute
{
    /// <summary>
    /// Initializes new instance of <see cref="ComposeDirectiveAttribute"/>
    /// </summary>
    /// <param name="name">
    /// Name of the directive that should be preserved in the supergraph composition.
    /// </param>
    public ComposeDirectiveAttribute(string name)
    {
        Name = name;
    }

    /// <summary>
    /// Gets the composed directive name.
    /// </summary>
    public string Name { get; }

    public override void OnConfigure(IDescriptorContext context, ISchemaTypeDescriptor descriptor, Type type)
    {
        if (string.IsNullOrEmpty(Name))
        {
            throw ComposeDirective_Name_CannotBeEmpty(type);
        }
        descriptor.ComposeDirective(Name);
    }
}
