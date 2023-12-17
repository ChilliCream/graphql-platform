using HotChocolate.ApolloFederation.Constants;
using HotChocolate.ApolloFederation.Properties;

namespace HotChocolate.ApolloFederation;

/// <summary>
/// <code>
/// directive @policy(policies: [[Policy!]!]!) on
///   | FIELD_DEFINITION
///   | OBJECT
///   | INTERFACE
///   | SCALAR
///   | ENUM
/// </code>
/// Indicates to composition that the target element is restricted based on authorization policies
/// that are evaluated in a Rhai script or coprocessor.
/// <see cref="https://www.apollographql.com/docs/router/configuration/authorization#policy"/>
/// </summary>
public sealed class PolicyDirectiveType : DirectiveType
{
    protected override void Configure(IDirectiveTypeDescriptor descriptor)
        => descriptor
            .Name(WellKnownTypeNames.PolicyDirective)
            .Description(FederationResources.PolicyDirective_Description)
            .Location(
                DirectiveLocation.FieldDefinition |
                DirectiveLocation.Object |
                DirectiveLocation.Interface |
                DirectiveLocation.Scalar |
                DirectiveLocation.Enum);
}
