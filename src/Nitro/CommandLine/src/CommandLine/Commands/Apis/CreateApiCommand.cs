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
        Options.Add(Opt<OptionalWorkspaceIdOption>.Instance);
        Options.Add(Opt<ApiKindOption>.Instance);

        this.AddGlobalNitroOptions();

        this.SetActionWithExceptionHandling(console, async (parseResult, cancellationToken)
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
        parseResult.AssertHasAuthentication(sessionService);

        var workspaceId = parseResult.GetWorkspaceId(sessionService);

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

        await using (var activity = console.StartActivity($"Creating API '{name.EscapeMarkup()}'"))
        {
            var payload = await client.CreateApiAsync(workspaceId, path, name, kind, ct);

            if (payload.Errors?.Count > 0)
            {
                activity.Fail("Failed to create the API.");

                foreach (var mutationError in payload.Errors)
                {
                    var errorMessage = mutationError switch
                    {
                        IError mutationErrorDetail => "Unexpected mutation error: " + mutationErrorDetail.Message,
                        _ => "Unexpected mutation error."
                    };

                    console.Error.WriteErrorLine(errorMessage);
                    return ExitCodes.Error;
                }
            }

            var changeResult = payload.Changes?.SingleOrDefault();
            if (changeResult is null)
            {
                activity.Fail("Failed to create the API.");
                console.Error.WriteErrorLine("Could not create API.");
                return ExitCodes.Error;
            }

            if (changeResult.Error is IError error)
            {
                activity.Fail("Failed to create the API.");
                console.Error.WriteErrorLine(error.Message);
                return ExitCodes.Error;
            }

            if (changeResult.Result is not ICreateApiCommandMutation_Api result)
            {
                activity.Fail("Failed to create the API.");
                console.Error.WriteErrorLine("Could not create API.");
                return ExitCodes.Error;
            }

            activity.Success($"Created API '{name.EscapeMarkup()}'.");

            if (result is IApiDetailPrompt_Api detail)
            {
                resultHolder.SetResult(new ObjectResult(ApiDetailPrompt.From(detail).ToObject()));
            }

            return ExitCodes.Success;
        }
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
