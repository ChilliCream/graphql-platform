using HotChocolate.ApolloFederation.Constants;
using HotChocolate.ApolloFederation.Properties;

namespace HotChocolate.ApolloFederation;

/// <summary>
/// <code>
/// directive @external on FIELD_DEFINITION
/// </code>
/// 
/// The @external directive is used to mark a field as owned by another service.
/// This allows service A to use fields from service B while also knowing at runtime
/// the types of that field. All the external fields should either be referenced from the @key, 
/// @requires or @provides directives field sets.
///
/// Due to the smart merging of entity types, Federation v2 no longer requires @external directive
/// on @key fields and can be safely omitted from the schema. @external directive is only required
/// on fields referenced by the @requires and @provides directive.
///
/// <example>
/// type Foo @key(fields: "id") {
///   id: ID!
///   remoteField: String @external
///   localField: String @requires(fields: "remoteField")
/// }
/// </example>
/// </summary>
public sealed class ExternalDirectiveType : DirectiveType
{
    protected override void Configure(IDirectiveTypeDescriptor descriptor)
        => descriptor
            .Name(WellKnownTypeNames.External)
            .Description(FederationResources.ExternalDirective_Description)
            .Location(DirectiveLocation.FieldDefinition);
}

/// <summary>
/// <code>
/// directive @extends on OBJECT | INTERFACE
/// </code>
/// 
/// The @extends directive is used to represent type extensions in the schema. Federated extended types should have 
/// corresponding @key directive defined that specifies primary key required to fetch the underlying object.
/// <example>
/// # extended from the Users service
/// type Foo @extends @key(fields: "id") {
///   id: ID
///   description: String
/// }
/// </example>
/// </summary>
public sealed class ExtendsDirectiveType : DirectiveType
{
    protected override void Configure(IDirectiveTypeDescriptor descriptor)
        => descriptor
            .Name(WellKnownTypeNames.Extends)
            .Description(FederationResources.ExtendsDirective_Description)
            .Location(DirectiveLocation.Object | DirectiveLocation.Interface);
}