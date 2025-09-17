namespace ChilliCream.Nitro.CommandLine.Cloud.Option;

internal sealed class ClientFormatOption : Option<string>
{
    public ClientFormatOption() : base("--format")
    {
        Description = "The format of which the client is stored.";
        this.FromAmong(ClientFormat.Relay, ClientFormat.Folder);
    }
}
