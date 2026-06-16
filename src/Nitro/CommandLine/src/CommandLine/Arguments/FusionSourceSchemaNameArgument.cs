namespace ChilliCream.Nitro.CommandLine.Arguments;

internal sealed class FusionSourceSchemaNameArgument : Argument<string>
{
    public const string ArgumentName = "SOURCE_SCHEMA_NAME";

    public FusionSourceSchemaNameArgument() : base(ArgumentName)
    {
        Description = "The name of the source schema to remove";
    }
}
