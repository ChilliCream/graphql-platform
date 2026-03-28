namespace ChilliCream.Nitro.CommandLine.Options;

internal class ClientIdOption : Option<string>
{
    public ClientIdOption() : base("--client-id")
    {
        Description = "The ID of the client";
        Required = true;
        this.DefaultFromEnvironmentValue("CLIENT_ID");
        this.NonEmptyStringsOnly();
    }
}
