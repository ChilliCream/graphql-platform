using ChilliCream.Nitro.Client;

namespace ChilliCream.Nitro.CommandLine.Commands.Clients.Components;

internal sealed class ClientDetailPrompt
{
    private readonly IClientDetailPrompt_Client _data;

    private ClientDetailPrompt(IClientDetailPrompt_Client data)
    {
        _data = data;
    }

    public ClientDetailPromptResult ToObject()
    {
        return new ClientDetailPromptResult
        {
            Id = _data.Id,
            Name = _data.Name,
            Api = _data.Api is { } api
                ? new Api { Name = api.Name }
                : null
        };
    }

    public static ClientDetailPrompt From(IClientDetailPrompt_Client data)
        => new(data);

    public class ClientDetailPromptResult
    {
        public required string Id { get; init; }

        public required string Name { get; init; }

        public required Api? Api { get; init; }
    }

    public class Api
    {
        public required string Name { get; init; }
    }
}
