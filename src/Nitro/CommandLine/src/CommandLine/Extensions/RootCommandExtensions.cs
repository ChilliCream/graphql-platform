using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Sessions;

namespace ChilliCream.Nitro.CommandLine;

internal static class RootCommandExtensions
{
    public static async Task<int> ExecuteAsync(
        this RootCommand rootCommand,
        IReadOnlyList<string> args,
        IServiceProvider services,
        CancellationToken cancellationToken)
    {
        // Parse command
        var parseResult = rootCommand.Parse(args);

        // Initialize session
        await services
            .GetRequiredService<ISessionService>()
            .LoadSessionAsync(cancellationToken);

        // Execute command
        var exitCode = await parseResult.InvokeAsync(cancellationToken: cancellationToken);

        // Print result
        var resultHolder = services.GetRequiredService<ResultHolder>();
        var formatter = services.GetRequiredService<IResultFormatter>();
        var console = services.GetRequiredService<INitroConsole>();
        var format = parseResult.GetValue(Opt<OutputFormatOption>.Instance);

        if (resultHolder.Result is { } result)
        {
            formatter.Format(result);
        }
        else if (format is OutputFormat.Json && exitCode == 0)
        {
            console.WriteLine("{}");
        }

        return exitCode;
    }
}
