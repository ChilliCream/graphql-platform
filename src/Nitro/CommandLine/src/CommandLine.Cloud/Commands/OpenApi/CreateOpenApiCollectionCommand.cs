using System.CommandLine.Invocation;
using ChilliCream.Nitro.CommandLine.Cloud.Client;
using ChilliCream.Nitro.CommandLine.Cloud.Commands.OpenApi.Options;
using ChilliCream.Nitro.CommandLine.Cloud.Option;
using ChilliCream.Nitro.CommandLine.Cloud.Option.Binders;
using ChilliCream.Nitro.CommandLine.Cloud.Results;
using static ChilliCream.Nitro.CommandLine.Cloud.ThrowHelper;

namespace ChilliCream.Nitro.CommandLine.Cloud.Commands.OpenApi;

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
        console.WriteLine("Creating an OpenAPI collection");
        console.WriteLine();

        const string apiMessage = "For which api do you want to create an OpenAPI collection?";
        var apiId = await context.GetOrSelectApiId(apiMessage);

        var name = await context
            .OptionOrAskAsync("Name", Opt<OpenApiCollectionNameOption>.Instance, cancellationToken);

        var input = new CreateOpenApiCollectionInput { Name = name, ApiId = apiId };
        var result =
            await client.CreateOpenApiCollectionCommandMutation.ExecuteAsync(input, cancellationToken);

        console.EnsureNoErrors(result);
        var data = console.EnsureData(result);
        console.PrintErrorsAndExit(data.CreateOpenApiCollection.Errors);

        var createdOpenApiCollection = data.CreateOpenApiCollection.OpenApiCollection;
        if (createdOpenApiCollection is null)
        {
            throw Exit("Could not create OpenAPI collection.");
        }

        console.OkLine($"OpenAPI collection {createdOpenApiCollection.Name.AsHighlight()} created.");

        if (createdOpenApiCollection is IOpenApiCollectionDetailPrompt_OpenApiCollection detail)
        {
            context.SetResult(OpenApiCollectionDetailPrompt.From(detail).ToObject([]));
        }

        return ExitCodes.Success;
    }
}
