namespace ChilliCream.Nitro.CommandLine;

internal sealed class OptionalSourceSchemaNameOption : Option<string>
{
    public const string OptionName = "--name";

    public OptionalSourceSchemaNameOption() : base(OptionName)
    {
        Description = "The name that identifies the source schema in the composite schema";
        this.NonEmptyStringsOnly();
    }
}
