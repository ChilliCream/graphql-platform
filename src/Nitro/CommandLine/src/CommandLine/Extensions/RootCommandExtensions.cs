using ChilliCream.Nitro.Client.Exceptions;
using ChilliCream.Nitro.CommandLine.Helpers;
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
        var console = services.GetRequiredService<INitroConsole>();

        // Parse command
        var parseResult = rootCommand.Parse(args);

        // Initialize session
        await services
            .GetRequiredService<ISessionService>()
            .LoadSessionAsync(cancellationToken);

        // Execute command
        int exitCode;

        try
        {
            exitCode = await parseResult.InvokeAsync(cancellationToken: cancellationToken);
        }
        catch (ExitException exception)
        {
            await console.Error.WriteLineAsync(exception.Message);

            return ExitCodes.Error;
        }
        catch (NitroClientException exception)
        {
            await console.Error.WriteLineAsync(exception.Message);

            return ExitCodes.Error;
        }
        catch (Exception ex) when (ex is OperationCanceledException or TaskCanceledException)
        {
            return ExitCodes.Error;
        }
        catch (Exception exception)
        {
            await console.Error.WriteLineAsync(exception.Message);

            return ExitCodes.Error;
        }

        // Print result
        var resultHolder = services.GetRequiredService<IResultHolder>();
        var formatter = services.GetRequiredService<IResultFormatter>();
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
