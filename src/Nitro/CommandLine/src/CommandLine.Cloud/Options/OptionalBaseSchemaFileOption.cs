namespace ChilliCream.Nitro.CLI.Option;

internal sealed class OptionalBaseSchemaFileOption : BaseSchemaFileOption
{
    public OptionalBaseSchemaFileOption() : base()
    {
        IsRequired = false;
    }
}
