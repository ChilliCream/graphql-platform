using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Helpers;

namespace HotChocolate.ApolloFederation;

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
[AttributeUsage(
    AttributeTargets.Class
    | AttributeTargets.Enum
    | AttributeTargets.Interface
    | AttributeTargets.Method
    | AttributeTargets.Property
    | AttributeTargets.Struct,
    AllowMultiple = true
)]
public sealed class RequiresScopesAttribute : DescriptorAttribute
{
    /// <summary>
    /// Initializes new instance of <see cref="RequiresScopesAttribute"/>
    /// </summary>
    /// <param name="scopes">
    /// Array of required JWT scopes.
    /// </param>
    public RequiresScopesAttribute(string[] scopes)
    {
        Scopes = scopes;
    }

    /// <summary>
    /// Retrieves array of required JWT scopes.
    /// </summary>
    public string[] Scopes { get; }

    protected internal override void TryConfigure(
        IDescriptorContext context,
        IDescriptor descriptor,
        ICustomAttributeProvider element)
    {
        void AddScopes(
            IHasDirectiveDefinition definition)
        {
            var existingScopes = definition
                .Directives
                .Select(t => t.Value)
                .OfType<RequiresScopes>()
                .FirstOrDefault();

            if (existingScopes is null)
            {
                existingScopes = new(new());
                definition.AddDirective(existingScopes, context.TypeInspector);
            }

            var newScopes = Scopes.Select(s => new Scope(s)).ToList();
            existingScopes.Scopes.Add(newScopes);
        }

        switch (descriptor)
        {
            case IEnumTypeDescriptor enumTypeDescriptor:
            {
                AddScopes(enumTypeDescriptor.ToDefinition());
                break;
            }
            case IObjectTypeDescriptor objectFieldDescriptor:
            {
                AddScopes(objectFieldDescriptor.ToDefinition());
                break;
            }
            case IObjectFieldDescriptor objectFieldDescriptor:
            {
                AddScopes(objectFieldDescriptor.ToDefinition());
                break;
            }
            case IInterfaceTypeDescriptor interfaceTypeDescriptor:
            {
                AddScopes(interfaceTypeDescriptor.ToDefinition());
                break;
            }
            case IInterfaceFieldDescriptor interfaceFieldDescriptor:
            {
                AddScopes(interfaceFieldDescriptor.ToDefinition());
                break;
            }
        }
    }
}
