using ChilliCream.Nitro.CLI.Option;

namespace ChilliCream.Nitro.CLI.Commands.Api.Options;

internal sealed class AllowBreakingSchemaChangesOption : Option<bool?>
{
    public AllowBreakingSchemaChangesOption() : base("--allow-breaking-schema-changes")
    {
        Description = "Allow breaking schema changes when no client breaks";
        IsRequired = false;
        this.DefaultFromEnvironmentValue("ALLOW_BREAKING_SCHEMA_CHANGES");
    }
}
