namespace ChilliCream.Nitro.CommandLine.Options;

internal sealed class ClientFormatOption : Option<string>
{
    public ClientFormatOption() : base("--format")
    {
        Description = "The format in which the client is stored.";
        SetDefaultValue(ClientFormat.Relay);
        this.FromAmong(ClientFormat.Relay, ClientFormat.Folder);
    }
}
