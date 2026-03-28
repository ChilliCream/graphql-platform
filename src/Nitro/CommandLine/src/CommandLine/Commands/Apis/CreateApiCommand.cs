using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Apis;
using ChilliCream.Nitro.CommandLine.Commands.Apis.Components;
using ChilliCream.Nitro.CommandLine.Commands.Apis.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using static System.StringSplitOptions;
using static ChilliCream.Nitro.CommandLine.ThrowHelper;

namespace ChilliCream.Nitro.CommandLine.Commands.Apis;

internal sealed class CreateApiCommand : Command
{
    public CreateApiCommand(
        INitroConsole console,
        IApisClient client,
        ISessionService sessionService,
        IResultHolder resultHolder) : base("create")
    {
        Description = "Creates a new API";

        Options.Add(Opt<ApiPathOption>.Instance);
        Options.Add(Opt<ApiNameOption>.Instance);
        Options.Add(Opt<WorkspaceIdOption>.Instance);
        Options.Add(Opt<ApiKindOption>.Instance);

        SetAction(async (parseResult, cancellationToken)
            => await ExecuteAsync(parseResult, console, client, sessionService, resultHolder, cancellationToken));
    }

    private static async Task<int> ExecuteAsync(
        ParseResult parseResult,
        INitroConsole console,
        IApisClient client,
        ISessionService sessionService,
        IResultHolder resultHolder,
        CancellationToken ct)
    {
        var workspaceId = parseResult.GetWorkspaceId(sessionService);

        console.WriteLine("Creating an API");

        var name = await console.PromptAsync("Name", defaultValue: null, parseResult, Opt<ApiNameOption>.Instance, ct);
        var pathResult = await console
            .PromptAsync(
                "Path [dim](e.g. /foo/bar)[/]",
                defaultValue: "/",
                parseResult,
                Opt<ApiPathOption>.Instance,
                ct);

        var path = pathResult.Split("/", TrimEntries | RemoveEmptyEntries);

        var kind = GetApiKind(parseResult);

        var payload = await client.CreateApiAsync(workspaceId, path, name, kind, ct);
        // console.PrintMutationErrorsAndExit(payload.Errors);

        var changeResult = payload.Changes?.SingleOrDefault();
        if (changeResult is null)
        {
            throw Exit("Could not create API.");
        }

        if (changeResult.Error is IError error)
        {
            throw Exit(error.Message);
        }

        if (changeResult.Result is not ICreateApiCommandMutation_Api api)
        {
            throw Exit("Could not create API.");
        }

        // console.OkLine(
        //     $"API [dim]{string.Join('/', api.Path)}[/]/{api.Name.AsHighlight()} created");

        if (changeResult.Result is IApiDetailPrompt_Api detail)
        {
            resultHolder.SetResult(new ObjectResult(ApiDetailPrompt.From(detail).ToObject()));
        }

        return ExitCodes.Success;
    }

    private static ApiKind? GetApiKind(ParseResult parseResult)
    {
        var kind = parseResult.GetValue(Opt<ApiKindOption>.Instance);

        return kind switch
        {
            "collection" => ApiKind.Collection,
            "service" => ApiKind.Service,
            "gateway" => ApiKind.Gateway,
            _ => null
        };
    }
}
