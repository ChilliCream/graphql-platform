namespace ChilliCream.Nitro.CommandLine.Options;

internal sealed class OptionalMockSchemaIdOption : MockSchemaIdOption
{
    public OptionalMockSchemaIdOption() : base()
    {
        IsRequired = false;
    }
}
