namespace ChilliCream.Nitro.CommandLine.Cloud.Option;

internal sealed class OptionalMockSchemaNameOption : MockSchemaNameOption
{
    public OptionalMockSchemaNameOption() : base()
    {
        IsRequired = false;
    }
}
