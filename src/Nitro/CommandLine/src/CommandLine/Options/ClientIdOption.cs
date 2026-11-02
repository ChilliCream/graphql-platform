namespace ChilliCream.Nitro.CommandLine;

internal class ClientIdOption : Option<string>
{
    public ClientIdOption() : base("--client-id")
    {
        Description = "The ID of the client";
        Required = true;
        this.DefaultFromEnvironmentValue(EnvironmentVariables.ClientId);
        this.NonEmptyStringsOnly();
    }
}
