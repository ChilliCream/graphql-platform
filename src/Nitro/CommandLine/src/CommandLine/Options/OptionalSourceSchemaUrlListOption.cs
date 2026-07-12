namespace ChilliCream.Nitro.CommandLine;

internal sealed class OptionalSourceSchemaUrlListOption : Option<List<string>>
{
    public const string OptionName = "--source-schema-url";

    public OptionalSourceSchemaUrlListOption()
        : base(OptionName)
    {
        Description = "A URL from which to download a source schema";
        Arity = ArgumentArity.ExactlyOne;
        AllowMultipleArgumentsPerToken = false;
    }
}
