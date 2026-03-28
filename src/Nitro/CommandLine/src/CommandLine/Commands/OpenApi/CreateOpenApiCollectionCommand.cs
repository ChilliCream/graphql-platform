using System.CommandLine.Invocation;
using ChilliCream.Nitro.Client.OpenApi;
using ChilliCream.Nitro.CommandLine.Commands.Apis.Inputs;
using ChilliCream.Nitro.CommandLine.Commands.OpenApi.Components;
using ChilliCream.Nitro.CommandLine.Commands.OpenApi.Options;
using ChilliCream.Nitro.CommandLine.Configuration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Results;
using static ChilliCream.Nitro.CommandLine.ThrowHelper;

namespace ChilliCream.Nitro.CommandLine.Commands.OpenApi;

internal sealed class CreateOpenApiCollectionCommand : Command
{
    public CreateOpenApiCollectionCommand() : base("create")
    {
        Description = "Creates a new OpenAPI collection";

        AddOption(Opt<OptionalApiIdOption>.Instance);
        AddOption(Opt<OpenApiCollectionNameOption>.Instance);

        this.SetHandler(
            ExecuteAsync,
            Bind.FromServiceProvider<InvocationContext>(),
            Bind.FromServiceProvider<IAnsiConsole>(),
            Bind.FromServiceProvider<IOpenApiClient>(),
            Bind.FromServiceProvider<CancellationToken>());
    }

    private static async Task<int> ExecuteAsync(
        InvocationContext context,
        IAnsiConsole console,
        IOpenApiClient client,
        CancellationToken cancellationToken)
    {
        console.WriteLine();
        console.WriteLine("Creating an OpenAPI collection");
        console.WriteLine();

        const string apiMessage = "For which API do you want to create an OpenAPI collection?";
        var apiId = await context.GetOrSelectApiId(apiMessage);

        var name = await context
            .OptionOrAskAsync("Name", Opt<OpenApiCollectionNameOption>.Instance, cancellationToken);

        var result = await client.CreateOpenApiCollectionAsync(
            apiId,
            name,
            cancellationToken);

        console.PrintMutationErrorsAndExit(result.Errors);

        if (result.OpenApiCollection is not {} openApiCollection)
        {
            throw Exit("Could not create OpenAPI collection.");
        }

        console.OkLine($"OpenAPI collection {openApiCollection.Name.AsHighlight()} created.");
        context.SetResult(OpenApiCollectionDetailPrompt.From(openApiCollection).ToObject());

        return ExitCodes.Success;
    }
}
