namespace ChilliCream.Nitro.CommandLine;

internal sealed class ClientFormatOption : Option<string>
{
    public ClientFormatOption() : base("--format")
    {
        Description = "The format in which the client is stored";
        DefaultValueFactory = _ => ClientFormat.Relay;
        AcceptOnlyFromAmong(ClientFormat.Relay, ClientFormat.Folder);
    }
}
