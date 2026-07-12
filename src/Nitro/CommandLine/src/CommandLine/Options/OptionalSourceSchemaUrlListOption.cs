namespace ChilliCream.Nitro.CommandLine;

internal sealed class OptionalSourceSchemaUrlListOption : Option<List<string>>
{
    public const string OptionName = "--source-schema-url";

    public OptionalSourceSchemaUrlListOption()
        : base(OptionName)
    {
        Description = "A source schema URL followed by its source schema settings file";
        HelpName = "url> <settings-file";
        Arity = new ArgumentArity(2, 2);
        AllowMultipleArgumentsPerToken = true;
    }
}
