using System.Collections.Generic;
using HotChocolate.ApolloFederation.Constants;
using HotChocolate.ApolloFederation.Properties;
using HotChocolate.ApolloFederation.Types;

namespace HotChocolate.ApolloFederation;

/// <summary>
/// <code>
/// directive @@requiresScopes(scopes: [[Scope!]!]!) on
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
public sealed class RequiresScopesDirectiveType : DirectiveType<RequiresScopes>
{
    protected override void Configure(IDirectiveTypeDescriptor<RequiresScopes> descriptor)
        => descriptor
            .BindArgumentsImplicitly()
            .Name(WellKnownTypeNames.RequiresScopes)
            .Description(FederationResources.RequiresScopesDirective_Description)
            .Location(
                DirectiveLocation.Enum |
                DirectiveLocation.FieldDefinition |
                DirectiveLocation.Interface |
                DirectiveLocation.Object |
                DirectiveLocation.Scalar);
}

/// <summary>
/// Object representation of @requiresScopes directive.
/// </summary>
public sealed class RequiresScopes
{
    /// <summary>
    /// Initializes new instance of <see cref="RequiresScopes"/>
    /// </summary>
    /// <param name="scopes">
    /// List of a list of required JWT scopes.
    /// </param>
    public RequiresScopes(List<List<Scope>> scopes)
    {
        Scopes = scopes;
    }

    /// <summary>
    /// Retrieves list of a list of required JWT scopes.
    /// </summary>
    public List<List<Scope>> Scopes { get; }
}
