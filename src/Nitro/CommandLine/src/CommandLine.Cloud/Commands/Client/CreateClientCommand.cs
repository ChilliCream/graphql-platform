using System.CommandLine.Invocation;
using ChilliCream.Nitro.CLI.Client;
using ChilliCream.Nitro.CLI.Option;
using ChilliCream.Nitro.CLI.Option.Binders;
using ChilliCream.Nitro.CLI.Results;
using ChilliCream.Nitro.CommandLine;
using static ChilliCream.Nitro.CLI.ThrowHelper;

namespace ChilliCream.Nitro.CLI.Commands.Client;

internal sealed class CreateClientCommand : Command
{
    public CreateClientCommand() : base("create")
    {
        Description = "Creates a new client";

        AddOption(Opt<OptionalApiIdOption>.Instance);
        AddOption(Opt<ClientNameOption>.Instance);

        this.SetHandler(
            ExecuteAsync,
            Bind.FromServiceProvider<InvocationContext>(),
            Bind.FromServiceProvider<IAnsiConsole>(),
            Bind.FromServiceProvider<IApiClient>(),
            Bind.FromServiceProvider<CancellationToken>());
    }

    private static async Task<int> ExecuteAsync(
        InvocationContext context,
        IAnsiConsole console,
        IApiClient client,
        CancellationToken cancellationToken)
    {
        console.WriteLine();
        console.WriteLine("Creating a client");
        console.WriteLine();

        const string apiMessage = "For which api do you want to create a client?";
        var apiId = await context.GetOrSelectApiId(apiMessage);

        var name = await context
            .OptionOrAskAsync("Name", Opt<ClientNameOption>.Instance, cancellationToken);

        var input = new CreateClientInput { Name = name, ApiId = apiId };
        var result =
            await client.CreateClientCommandMutation.ExecuteAsync(input, cancellationToken);

        console.EnsureNoErrors(result);
        var data = console.EnsureData(result);
        console.PrintErrorsAndExit(data.CreateClient.Errors);

        var createdClient = data.CreateClient.Client;
        if (createdClient is null)
        {
            throw Exit("Could not create api.");
        }

        console.OkLine($"Client {createdClient.Name.AsHighlight()} created.");

        if (createdClient is IClientDetailPrompt_Client detail)
        {
            var formatted = await ClientDetailPrompt.From(detail, client).ToObject([]);
            context.SetResult(formatted);
        }

        return ExitCodes.Success;
    }
}
