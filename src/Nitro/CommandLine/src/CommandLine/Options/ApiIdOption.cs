namespace ChilliCream.Nitro.CommandLine.Options;

internal class ApiIdOption : Option<string>
{
    public ApiIdOption() : base("--api-id", "The ID of the API")
    {
        IsRequired = true;
        this.DefaultFromEnvironmentValue("API_ID");
        this.NonEmptyStringsOnly();
    }
}
