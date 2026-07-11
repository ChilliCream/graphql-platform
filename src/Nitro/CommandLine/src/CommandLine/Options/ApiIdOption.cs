namespace ChilliCream.Nitro.CommandLine;

internal class ApiIdOption : Option<string>
{
    public const string OptionName = "--api-id";

    public ApiIdOption() : base(OptionName)
    {
        Description = "The ID of the API";
        Required = true;
        this.DefaultFromEnvironmentValue(EnvironmentVariables.ApiId);
        this.NonEmptyStringsOnly();
    }
}
