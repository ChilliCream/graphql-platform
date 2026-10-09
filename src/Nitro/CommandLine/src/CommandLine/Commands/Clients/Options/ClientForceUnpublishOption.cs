namespace ChilliCream.Nitro.CommandLine.Commands.Clients.Options;

internal sealed class ClientForceUnpublishOption : Option<bool>
{
    public const string OptionName = "--force";

    public ClientForceUnpublishOption() : base(OptionName)
    {
        Description = "Unpublish the client version even if an unpublish protection rule protects it";
        Required = false;
    }
}
