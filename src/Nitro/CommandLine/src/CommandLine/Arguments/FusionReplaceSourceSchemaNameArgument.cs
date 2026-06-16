namespace ChilliCream.Nitro.CommandLine.Arguments;

internal sealed class FusionReplaceSourceSchemaNameArgument : Argument<string>
{
    public const string ArgumentName = "OLD_SOURCE_SCHEMA_NAME";

    public FusionReplaceSourceSchemaNameArgument() : base(ArgumentName)
    {
        Description = "The name of the source schema to replace";
    }
}
