namespace HotChocolate.ApolloFederation.Types;

internal static class Descriptions
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
}