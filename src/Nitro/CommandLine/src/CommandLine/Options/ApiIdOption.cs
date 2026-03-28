namespace ChilliCream.Nitro.CommandLine.Options;

internal class ApiIdOption : Option<string>
{
    public const string OptionName = "--api-id";

    public ApiIdOption() : base(OptionName, "The ID of the API")
    {
        IsRequired = true;
        this.DefaultFromEnvironmentValue("API_ID");
        this.NonEmptyStringsOnly();
    }
}
