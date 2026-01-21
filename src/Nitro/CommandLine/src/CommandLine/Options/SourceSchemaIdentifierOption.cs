namespace ChilliCream.Nitro.CommandLine.Options;

internal sealed class SourceSchemaIdentifierOption : Option<List<string>>
{
    public SourceSchemaIdentifierOption() : base("--source-schema")
    {
        Description = CommandLineResources.PublishCommand_SourceSchemaOption_Description;

        AddAlias("-s");
    }
}
