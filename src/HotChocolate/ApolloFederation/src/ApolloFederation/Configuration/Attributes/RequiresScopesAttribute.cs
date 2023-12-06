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
public sealed class RequiresScopesAttribute : Attribute
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
}
