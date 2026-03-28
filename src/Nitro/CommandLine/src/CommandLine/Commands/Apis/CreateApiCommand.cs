using System.CommandLine.Invocation;
using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Apis;
using ChilliCream.Nitro.CommandLine.Commands.Apis.Components;
using ChilliCream.Nitro.CommandLine.Commands.Apis.Options;
using ChilliCream.Nitro.CommandLine.Configuration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using static System.StringSplitOptions;
using static ChilliCream.Nitro.CommandLine.ThrowHelper;

namespace ChilliCream.Nitro.CommandLine.Commands.Apis;

internal sealed class CreateApiCommand : Command
{
    public CreateApiCommand() : base("create")
    {
        Description = "Creates a new API";

        Options.Add(Opt<ApiPathOption>.Instance);
        Options.Add(Opt<ApiNameOption>.Instance);
        Options.Add(Opt<WorkspaceIdOption>.Instance);
        Options.Add(Opt<ApiKindOption>.Instance);

        this.SetHandler(
            ExecuteAsync,
            Bind.FromServiceProvider<InvocationContext>(),
            Bind.FromServiceProvider<INitroConsole>(),
            Bind.FromServiceProvider<IApisClient>(),
            Bind.FromServiceProvider<CancellationToken>());
    }

    private static async Task<int> ExecuteAsync(
        InvocationContext context,
        INitroConsole console,
        IApisClient client,
        CancellationToken ct)
    {
        var workspaceId = context.RequireWorkspaceId();

        console.WriteLine("Creating an API");

        var name = await context.OptionOrAskAsync("Name", Opt<ApiNameOption>.Instance, ct);
        var pathResult = await context
            .OptionOrAskAsync(
                "Path [dim](e.g. /foo/bar)[/]",
                Opt<ApiPathOption>.Instance,
                defaultValue: "/",
                ct);

        var path = pathResult.Split("/", TrimEntries | RemoveEmptyEntries);

        var kind = context.GetApiKind();

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
            context.SetResult(ApiDetailPrompt.From(detail).ToObject());
        }

        return ExitCodes.Success;
    }
}

file static class Extensions
{
    public static ApiKind? GetApiKind(
        this InvocationContext context)
    {
        var kind = context.ParseResult.GetValueForOption(Opt<ApiKindOption>.Instance);

        return kind switch
        {
            "collection" => ApiKind.Collection,
            "service" => ApiKind.Service,
            "gateway" => ApiKind.Gateway,
            _ => null
        };
    }
}
