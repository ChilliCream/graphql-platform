namespace ChilliCream.Nitro.CommandLine.Options;

internal class ApiIdOption : Option<string>
{
    public ApiIdOption() : base("--api-id", "The id of the api")
    {
        IsRequired = true;
        this.DefaultFromEnvironmentValue("API_ID");
    }
}

internal sealed class OptionalApiIdOption : ApiIdOption
{
    public OptionalApiIdOption() : base()
    {
        IsRequired = false;
    }
}
