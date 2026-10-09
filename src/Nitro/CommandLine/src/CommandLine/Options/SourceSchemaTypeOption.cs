namespace ChilliCream.Nitro.CommandLine;

internal sealed class SourceSchemaTypeOption : Option<string>
{
    public const string OptionName = "--schema-type";
    public const string GraphQLFederation = "graphql-federation";
    public const string ApolloFederation1 = "apollo-federation-1";
    public const string ApolloFederation2 = "apollo-federation-2";

    public SourceSchemaTypeOption() : base(OptionName)
    {
        Description =
            "The source schema specification. New settings default to GraphQL Federation";
        AcceptOnlyFromAmong(GraphQLFederation, ApolloFederation1, ApolloFederation2);
    }
}
