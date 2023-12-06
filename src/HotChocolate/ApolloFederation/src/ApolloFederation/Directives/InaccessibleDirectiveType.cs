using HotChocolate.ApolloFederation.Constants;
using HotChocolate.ApolloFederation.Properties;

namespace HotChocolate.ApolloFederation;

/// <summary>
/// <code>
/// directive @inaccessible on FIELD_DEFINITION
///  | OBJECT
///  | INTERFACE
///  | UNION
///  | ENUM
///  | ENUM_VALUE
///  | SCALAR
///  | INPUT_OBJECT
///  | INPUT_FIELD_DEFINITION
///  | ARGUMENT_DEFINITION
/// </code>
///
/// The @inaccessible directive is used to mark location within schema as inaccessible
/// from the GraphQL Router. Applying @inaccessible directive on a type is equivalent of applying
/// it on all type fields.
///
/// While @inaccessible fields are not exposed by the router to the clients, they are still available
/// for query plans and can be referenced from @key and @requires directives. This allows you to not
/// expose sensitive fields to your clients but still make them available for computations.
/// Inaccessible can also be used to incrementally add schema elements (e.g. fields) to multiple
/// subgraphs without breaking composition.
/// <example>
/// type Foo @inaccessible {
///   hiddenId: ID!
///   hiddenField: String
/// }
/// </example>
/// </summary>
public sealed class InaccessibleDirectiveType : DirectiveType
{
    protected override void Configure(IDirectiveTypeDescriptor descriptor)
        => descriptor
            .Name(WellKnownTypeNames.Inaccessible)
            .Description(FederationResources.InaccessibleDirective_Description)
            .Location(
                DirectiveLocation.FieldDefinition
                | DirectiveLocation.Object
                | DirectiveLocation.Interface
                | DirectiveLocation.Union
                | DirectiveLocation.Enum
                | DirectiveLocation.EnumValue
                | DirectiveLocation.Scalar
                | DirectiveLocation.Scalar
                | DirectiveLocation.InputObject
                | DirectiveLocation.InputFieldDefinition
                | DirectiveLocation.ArgumentDefinition);
}
