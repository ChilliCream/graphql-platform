namespace ChilliCream.Nitro.CommandLine.Options;

internal sealed class SourceSchemaIdentifierListOption : Option<List<string>>
{
    public SourceSchemaIdentifierListOption() : base("--source-schema")
    {
        Description = "One or more source schemas that should be included in the composition. Source schemas can either be just a name ('example') or a name and a version ('example@1.0.0'). If no version is specified the value of the '--tag' option is taken as the source schema version.";

        AddAlias("-s");
    }
}
