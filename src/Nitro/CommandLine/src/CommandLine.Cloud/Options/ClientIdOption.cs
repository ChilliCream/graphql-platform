namespace ChilliCream.Nitro.CommandLine.Cloud.Option;

internal sealed class ClientIdOption : Option<string>
{
    public ClientIdOption() : base("--client-id")
    {
        Description = "The id of the client";
        IsRequired = true;
        this.DefaultFromEnvironmentValue("CLIENT_ID");
    }
}
