namespace ChilliCream.Nitro.CLI.Option;

internal sealed class OptionalMockSchemaIdOption : MockSchemaIdOption
{
    public OptionalMockSchemaIdOption() : base()
    {
        IsRequired = false;
    }
}
