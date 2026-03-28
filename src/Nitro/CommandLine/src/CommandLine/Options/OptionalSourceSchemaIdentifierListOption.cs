namespace ChilliCream.Nitro.CommandLine.Options;

internal sealed class OptionalSourceSchemaIdentifierListOption : Option<List<string>>
{
    public const string OptionName = "--source-schema";

    public OptionalSourceSchemaIdentifierListOption() : base(OptionName)
    {
        Description =
            "One or more source schemas that should be included in the composition. Source schemas can either be just a name ('example') or a name and a version ('example@1.0.0'). If no version is specified the value of the '"
            + TagOption.OptionName
            + "' option is taken as the source schema version.";

        Aliases.Add("-s");
    }
}
