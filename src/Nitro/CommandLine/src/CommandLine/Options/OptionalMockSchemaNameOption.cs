namespace ChilliCream.Nitro.CommandLine;

internal sealed class OptionalMockSchemaNameOption : MockSchemaNameOption
{
    public OptionalMockSchemaNameOption() : base()
    {
        Required = false;
    }
}
