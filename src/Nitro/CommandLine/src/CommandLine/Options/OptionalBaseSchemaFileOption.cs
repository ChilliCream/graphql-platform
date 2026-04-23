namespace ChilliCream.Nitro.CommandLine;

internal sealed class OptionalBaseSchemaFileOption : BaseSchemaFileOption
{
    public OptionalBaseSchemaFileOption() : base()
    {
        Required = false;
    }
}
