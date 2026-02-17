namespace ChilliCream.Nitro.CommandLine.Options;

internal sealed class OptionalMockSchemaNameOption : MockSchemaNameOption
{
    public OptionalMockSchemaNameOption() : base()
    {
        IsRequired = false;
    }
}
