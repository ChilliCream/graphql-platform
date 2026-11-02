namespace ChilliCream.Nitro.CommandLine.Commands.Apis.Options;

internal sealed class AllowBreakingSchemaChangesOption : Option<bool?>
{
    public AllowBreakingSchemaChangesOption() : base("--allow-breaking-schema-changes")
    {
        Description = "Allow breaking schema changes when no client breaks";
        Required = false;
        this.DefaultFromEnvironmentValue(EnvironmentVariables.AllowBreakingSchemaChanges);
    }
}
