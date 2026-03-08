namespace ChilliCream.Nitro.CommandLine.Options;

internal sealed class ClientIdOption : Option<string>
{
    public ClientIdOption() : base("--client-id")
    {
        Description = "The ID of the client";
        IsRequired = true;
        this.DefaultFromEnvironmentValue("CLIENT_ID");
    }
}
