using ChilliCream.Nitro.CommandLine.Commands.Clients.Components;

namespace ChilliCream.Nitro.CommandLine.Options;

internal sealed class ClientDetailFieldsOption : Option<IEnumerable<string>>
{
    public ClientDetailFieldsOption() : base("--fields")
    {
        Description = "The fields to display in the client detail prompt.";
        AllowMultipleArgumentsPerToken = true;
        this.FromAmong(ClientDetailFields.Versions, ClientDetailFields.PublishedVersions);
    }
}
