namespace ChilliCream.Nitro.CommandLine;

internal sealed class OptionalRemoveSourceSchemaListOption : Option<List<string>>
{
    public const string OptionName = "--remove-source-schema";

    public OptionalRemoveSourceSchemaListOption() : base(OptionName)
    {
        Description = "One or more source schemas to remove from the archive before composing.";
        AllowMultipleArgumentsPerToken = true;
    }
}
