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
}
