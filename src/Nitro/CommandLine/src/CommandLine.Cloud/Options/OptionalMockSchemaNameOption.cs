namespace ChilliCream.Nitro.CLI.Option;

internal sealed class OptionalMockSchemaNameOption : MockSchemaNameOption
{
    public OptionalMockSchemaNameOption() : base()
    {
        IsRequired = false;
    }
}
