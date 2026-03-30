using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
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
        var console = services.GetRequiredService<INitroConsole>();

        // Parse command
        var parseResult = rootCommand.Parse(args);

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
            console.WriteLine();
            formatter.Format(result);
        }
        else if (format is OutputFormat.Json && exitCode == 0)
        {
            console.WriteDirectly("{}");
        }

        return exitCode;
    }

    private static void ConfigureClientContext(
        NitroClientContext context,
        ParseResult parseResult,
        Session? session)
    {
        var cloudUrlResult = parseResult.GetResult(Opt<OptionalCloudUrlOption>.Instance);
        var cloudUrl = parseResult.GetValue(Opt<OptionalCloudUrlOption>.Instance);
        string? apiUrl;
        if (cloudUrlResult is { Implicit: false } && !string.IsNullOrWhiteSpace(cloudUrl))
        {
            apiUrl = cloudUrl;
        }
        else
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
