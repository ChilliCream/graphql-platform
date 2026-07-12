namespace ChilliCream.Nitro.CommandLine;

internal sealed class OptionalSourceSchemaUrlOption : Option<string>
{
    public const string OptionName = "--source-schema-url";

    public OptionalSourceSchemaUrlOption()
        : base(OptionName)
    {
        Description = "A URL from which to download a source schema";
        Arity = ArgumentArity.ExactlyOne;
        AllowMultipleArgumentsPerToken = false;
    }
}
