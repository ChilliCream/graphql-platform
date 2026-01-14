using ChilliCream.Nitro.CommandLine.Options;

namespace ChilliCream.Nitro.CommandLine.Commands.Apis.Options;

internal sealed class AllowBreakingSchemaChangesOption : Option<bool?>
{
    public AllowBreakingSchemaChangesOption() : base("--allow-breaking-schema-changes")
    {
        Description = "Allow breaking schema changes when no client breaks";
        IsRequired = false;
        this.DefaultFromEnvironmentValue("ALLOW_BREAKING_SCHEMA_CHANGES");
    }
}
