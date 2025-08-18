namespace ChilliCream.Nitro.CommandLine.Cloud.Option;

internal sealed class OptionalMockSchemaIdOption : MockSchemaIdOption
{
    public OptionalMockSchemaIdOption() : base()
    {
        IsRequired = false;
    }
}
