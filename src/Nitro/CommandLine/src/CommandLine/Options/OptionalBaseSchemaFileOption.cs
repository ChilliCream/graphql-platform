namespace ChilliCream.Nitro.CommandLine.Options;

internal sealed class OptionalBaseSchemaFileOption : BaseSchemaFileOption
{
    public OptionalBaseSchemaFileOption() : base()
    {
        IsRequired = false;
    }
}
