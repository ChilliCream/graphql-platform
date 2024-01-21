namespace HotChocolate.ApolloFederation.Properties;

internal partial class FederationResources
{
    public const string ProvidesDirective_Description =
        "Used to annotate the expected returned fieldset from a field on a base type " +
        "that is guaranteed to be selectable by the federation gateway.";

    public const string KeyDirective_Description =
        "Used to indicate a combination of fields that can be used to uniquely identify " +
        "and fetch an object or interface.";

    public const string ExternalDirective_Description =
        "Directive to indicate that a field is owned by another service, " +
        "for example via Apollo federation.";

    public const string ExtendsDirective_Description =
        "Directive to indicate that marks target object as extending part of the federated schema.";
    
    public const string ContactDirective_Description =
        "Provides contact information of the owner responsible for this subgraph schema.";

    public const string RequiresDirective_Description =
        "Used to annotate the required input fieldset from a base type for a resolver.";
    
    public const string OverrideDirective_Description =
        "Overrides fields resolution logic from other subgraph. " +
        "Used for migrating fields from one subgraph to another.";
    
    public const string ShareableDirective_Description =
        "Indicates that given object and/or field can be resolved by multiple subgraphs.";

    public const string InterfaceObjectDirective_Description =
        "Provides meta information to the router that this entity type is an interface in the supergraph.";

    public const string InaccessibleDirective_Description =
        "Marks location within schema as inaccessible from the GraphQL Gateway.";

    public const string ComposeDirective_Description =
        "Marks underlying custom directive to be included in the Supergraph schema.";
    
    public const string ServiceType_Description =
        "This type provides a field named sdl: String! which exposes the SDL of the " +
        "service's schema. This SDL (schema definition language) is a printed version " +
        "of the service's schema including the annotations of federation directives. " +
        "This SDL does not include the additions of the federation spec.";

    public const string AuthenticatedDirective_Description =
        "Indicates to composition that the target element is accessible " +
        "only to the authenticated supergraph users.";
    
    public const string RequiresScopesDirective_Description =
        "Indicates to composition that the target element is accessible only " +
        "to the authenticated supergraph users with the appropriate JWT scopes.";
}