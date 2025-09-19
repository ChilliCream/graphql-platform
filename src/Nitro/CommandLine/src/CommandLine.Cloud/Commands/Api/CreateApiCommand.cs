using System.CommandLine.Invocation;
using ChilliCream.Nitro.CommandLine.Cloud.Client;
using ChilliCream.Nitro.CommandLine.Cloud.Commands.Api.Options;
using ChilliCream.Nitro.CommandLine.Cloud.Option;
using ChilliCream.Nitro.CommandLine.Cloud.Option.Binders;
using ChilliCream.Nitro.CommandLine.Cloud.Results;
using static System.StringSplitOptions;
using static ChilliCream.Nitro.CommandLine.Cloud.ThrowHelper;

namespace ChilliCream.Nitro.CommandLine.Cloud.Commands.Api;

internal sealed class CreateApiCommand : Command
{
    public CreateApiCommand() : base("create")
    {
        Description = "Creates a new api";

        AddOption(Opt<ApiPathOption>.Instance);
        AddOption(Opt<ApiNameOption>.Instance);
        AddOption(Opt<WorkspaceIdOption>.Instance);
        AddOption(Opt<ApiKindOption>.Instance);

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
        CancellationToken ct)
    {
        var workspaceId = context.RequireWorkspaceId();

        console.WriteLine();
        console.WriteLine("Creating a api");
        console.WriteLine();

        var name = await context.OptionOrAskAsync("Name", Opt<ApiNameOption>.Instance, ct);
        var pathResult = await context
            .OptionOrAskAsync(
                "Path [dim](e.g. /foo/bar)[/]",
                Opt<ApiPathOption>.Instance,
                defaultValue: "/",
                ct);

        var path = pathResult.Split("/", TrimEntries | RemoveEmptyEntries);

        var kind = context.GetApiKind();

        var result = await client.CreateApiCommandMutation
            .ExecuteAsync(workspaceId, path, name, kind, ct);

        console.EnsureNoErrors(result);
        var data = console.EnsureData(result);
        console.PrintErrorsAndExit(data.PushWorkspaceChanges.Errors);

        var changeResult = data.PushWorkspaceChanges.Changes?.SingleOrDefault();
        if (changeResult is null)
        {
            throw Exit("Could not create api.");
        }

        if (changeResult.Error is IError error)
        {
            throw Exit(error.Message);
        }

        if (changeResult.Result is not ICreateApiCommandMutation_Api api)
        {
            throw Exit("Could not create api.");
        }

        console.OkLine(
            $"Api [dim]{string.Join('/', api.Path)}[/]/{api.Name.AsHighlight()} created");

        if (changeResult.Result is IApiDetailPrompt_Api detail)
        {
            context.SetResult(ApiDetailPrompt.From(detail).ToObject());
        }

        return ExitCodes.Success;
    }
}

file static class Extensions
{
    public static ApiKind? GetApiKind(this InvocationContext context)
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
