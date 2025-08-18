namespace ChilliCream.Nitro.CommandLine.Cloud.Option;

internal sealed class OptionalBaseSchemaFileOption : BaseSchemaFileOption
{
    public OptionalBaseSchemaFileOption() : base()
    {
        IsRequired = false;
    }
}
