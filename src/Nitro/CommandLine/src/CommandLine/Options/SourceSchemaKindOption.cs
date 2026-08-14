namespace ChilliCream.Nitro.CommandLine;

internal sealed class SourceSchemaKindOption : Option<string>
{
    public const string OptionName = "--kind";
    public const string Generic = "generic";
    public const string HotChocolate = "hot-chocolate";
    public const string ApolloFederation = "apollo-federation";

    public SourceSchemaKindOption() : base(OptionName)
    {
        Description =
            "The kind of GraphQL server that serves the source schema. "
            + "When omitted, kind-specific settings are left unchanged";
        AcceptOnlyFromAmong(Generic, HotChocolate, ApolloFederation);
    }
}
