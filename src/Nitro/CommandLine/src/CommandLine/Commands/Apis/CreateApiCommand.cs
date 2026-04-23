using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Apis;
using ChilliCream.Nitro.CommandLine.Commands.Apis.Components;
using ChilliCream.Nitro.CommandLine.Commands.Apis.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using static System.StringSplitOptions;
using static ChilliCream.Nitro.CommandLine.ThrowHelper;

namespace ChilliCream.Nitro.CommandLine.Commands.Apis;

internal sealed class CreateApiCommand : Command
{
    public CreateApiCommand() : base("create")
    {
        Description = "Create a new API.";

        Options.Add(Opt<ApiPathOption>.Instance);
        Options.Add(Opt<ApiNameOption>.Instance);
        Options.Add(Opt<OptionalWorkspaceIdOption>.Instance);
        Options.Add(Opt<ApiKindOption>.Instance);

        this.AddGlobalNitroOptions();

        this.AddExamples(
            """
            api create --name "my-api"
            """);

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken ct)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var client = services.GetRequiredService<IApisClient>();
        var sessionService = services.GetRequiredService<ISessionService>();
        var resultHolder = services.GetRequiredService<IResultHolder>();

        parseResult.AssertHasAuthentication(sessionService);

        var workspaceId = parseResult.GetWorkspaceId(sessionService);

        var name = await console.PromptAsync("Name", defaultValue: null, parseResult, Opt<ApiNameOption>.Instance, ct);
        var pathResult = await console
            .PromptAsync(
                $"Path {"(e.g. /foo/bar)".Dim()}",
                defaultValue: "/",
                parseResult,
                Opt<ApiPathOption>.Instance,
                ct);

        if (!pathResult.StartsWith('/'))
        {
            throw new ExitException($"The path '{pathResult}' is invalid. It must start with '/'.");
        }

        var path = pathResult.Split("/", TrimEntries | RemoveEmptyEntries);

        var kind = GetApiKind(parseResult);

        await using (var activity = console.StartActivity(
            $"Creating API '{name.EscapeMarkup()}'",
            "Failed to create the API."))
        {
            var payload = await client.CreateApiAsync(workspaceId, path, name, kind, ct);

            if (payload.Errors?.Count > 0)
            {
                await activity.FailAllAsync();

                foreach (var mutationError in payload.Errors)
                {
                    var errorMessage = mutationError switch
                    {
                        IError mutationErrorDetail => "Unexpected mutation error: " + mutationErrorDetail.Message,
                        _ => Messages.UnexpectedMutationError()
                    };

                    console.Error.WriteErrorLine(errorMessage);
                    return ExitCodes.Error;
                }
            }

            var changeResult = payload.Changes?.SingleOrDefault();
            if (changeResult is null)
            {
                throw MutationReturnedNoData();
            }

            if (changeResult.Error is IError error)
            {
                await activity.FailAllAsync();
                console.Error.WriteErrorLine(error.Message);
                return ExitCodes.Error;
            }

            if (changeResult.Result is not ICreateApiCommandMutation_Api result)
            {
                throw MutationReturnedNoData();
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
