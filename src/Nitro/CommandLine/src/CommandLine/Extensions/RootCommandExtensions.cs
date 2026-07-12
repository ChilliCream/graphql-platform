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
        CommandExecutionContext.s_services.Value = new CommandServices(services);

        var console = services.GetRequiredService<INitroConsole>();

        // Parse command
        var parseResult = rootCommand.Parse(args);

        // Short-circuit on parse errors: let InvokeAsync report them and return the
        // corresponding exit code.
        if (parseResult.Errors.Count > 0)
        {
            return await parseResult.InvokeAsync(invocationConfiguration, cancellationToken);
        }

        var format = parseResult.GetValue(Opt<OptionalOutputFormatOption>.Instance);

        if (format.HasValue)
        {
            console.SetOutputFormat(format.Value);
        }

        // Initialize session
        await services
            .GetRequiredService<ISessionService>()
            .LoadSessionAsync(cancellationToken);

        var session = services.GetRequiredService<ISessionService>().Session;

        // Configure Nitro client context
        var context = services.GetRequiredService<NitroClientContext>();
        ConfigureClientContext(context, parseResult, session);

        // Execute command
        var exitCode = await parseResult.InvokeAsync(invocationConfiguration, cancellationToken);

        // Print result
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
            console.Out.WriteLine("{}");
        }

        return exitCode;
    }

    private static void ConfigureClientContext(
        NitroClientContext context,
        ParseResult parseResult,
        Session? session)
    {
        // The option resolves explicit --cloud-url, then the NITRO_CLOUD_URL env var.
        // We then fall back to the session's URL, and finally Constants.ApiUrl
        // (applied by NitroClientContext.Configure when apiUrl is null).
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
