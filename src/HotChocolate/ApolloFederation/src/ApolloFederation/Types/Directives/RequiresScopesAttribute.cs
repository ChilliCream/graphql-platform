using System.Reflection;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.ApolloFederation.Types;

/// <summary>
/// <code>
/// directive @requiresScopes(scopes: [[Scope!]!]!) on
///     ENUM
///   | FIELD_DEFINITION
///   | INTERFACE
///   | OBJECT
///   | SCALAR
/// </code>
///
/// Directive that is used to indicate that the target element is accessible only to the authenticated supergraph users with the appropriate JWT scopes.
/// Refer to the <see href = "https://www.apollographql.com/docs/router/configuration/authorization#requiresscopes"> Apollo Router article</see> for additional details.
/// <example>
/// type Foo @key(fields: "id") {
///   id: ID
///   description: String @requiresScopes(scopes: [["scope1"]])
/// }
/// </example>
/// </summary>
/// <remarks>
/// Initializes new instance of <see cref="RequiresScopesAttribute"/>
/// </remarks>
/// <param name="scopes">
/// Array of required JWT scopes.
/// </param>
[AttributeUsage(
    AttributeTargets.Class
    | AttributeTargets.Enum
    | AttributeTargets.Interface
    | AttributeTargets.Method
    | AttributeTargets.Property
    | AttributeTargets.Struct,
    AllowMultiple = true
)]
public sealed class RequiresScopesAttribute(string[] scopes) : DescriptorAttribute
{
    /// <summary>
    /// Retrieves array of required JWT scopes.
    /// </summary>
    public string[] Scopes { get; } = scopes;

    protected internal override void TryConfigure(
        IDescriptorContext context,
        IDescriptor descriptor,
        ICustomAttributeProvider element)
    {
        switch (descriptor)
        {
            case IEnumTypeDescriptor desc:
                desc.RequiresScopes(Scopes);
                break;

            case IObjectTypeDescriptor desc:
                desc.RequiresScopes(Scopes);
                break;

            case IObjectFieldDescriptor desc:
                desc.RequiresScopes(Scopes);
                break;

            case IInterfaceTypeDescriptor desc:
                desc.RequiresScopes(Scopes);
                break;

            case IInterfaceFieldDescriptor desc:
                desc.RequiresScopes(Scopes);
                break;
        }
    }
}
