using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Sessions;

namespace ChilliCream.Nitro.CommandLine;

internal static class RootCommandExtensions
{
    public static async Task<int> ExecuteAsync(
        this RootCommand rootCommand,
        IReadOnlyList<string> args,
        IServiceProvider services,
        InvocationConfiguration? invocationConfiguration,
        CancellationToken cancellationToken)
    {
        CommandExecutionContext.Initialize(new CommandServices(services));

        var console = services.GetRequiredService<INitroConsole>();

        var parseResult = rootCommand.Parse(args);

        // On parse errors, delegate to InvokeAsync to report them and return the exit code.
        if (parseResult.Errors.Count > 0)
        {
            return await parseResult.InvokeAsync(invocationConfiguration, cancellationToken);
        }

        var format = parseResult.GetValue(Opt<OptionalOutputFormatOption>.Instance);

        if (format.HasValue)
        {
            console.SetOutputFormat(format.Value);
        }

        await services
            .GetRequiredService<ISessionService>()
            .LoadSessionAsync(cancellationToken);

        var session = services.GetRequiredService<ISessionService>().Session;

        var context = services.GetRequiredService<NitroClientContext>();
        ConfigureClientContext(context, parseResult, session);

        var exitCode = await parseResult.InvokeAsync(invocationConfiguration, cancellationToken);

        var resultHolder = services.GetRequiredService<IResultHolder>();
        var formatter = services.GetRequiredService<IResultFormatter>();

        if (resultHolder.Result is { } result)
        {
            if (console.HasWrittenOutput)
            {
                console.WriteLine();
            }

            formatter.Format(result);
        }
        else if (format is OutputFormat.Json && exitCode == 0)
        {
            console.WriteRawLine("{}");
        }

        return exitCode;
    }

    private static void ConfigureClientContext(
        NitroClientContext context,
        ParseResult parseResult,
        Session? session)
    {
        // Resolution order: explicit --cloud-url, then NITRO_CLOUD_URL, then the session's
        // URL, then Constants.ApiUrl (via NitroClientContext.Configure).
        var apiUrl = parseResult.GetValue(Opt<OptionalCloudUrlOption>.Instance);

        if (string.IsNullOrWhiteSpace(apiUrl))
        {
            apiUrl = session?.ApiUrl;
        }

        var apiKey = parseResult.GetValue(Opt<OptionalApiKeyOption>.Instance);
        INitroClientAuthorization? auth;
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            auth = new NitroClientApiKeyAuthorization(apiKey);
        }
        else if (session?.Tokens?.AccessToken is { } token)
        {
            auth = new NitroClientAccessTokenAuthorization(token);
        }
        else
        {
            auth = null;
        }

        context.Configure(apiUrl, auth);
    }
}
