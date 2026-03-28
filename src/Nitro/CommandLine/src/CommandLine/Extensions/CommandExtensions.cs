using ChilliCream.Nitro.Client.Exceptions;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Results;

namespace ChilliCream.Nitro.CommandLine;

internal static class CommandExtensions
{
    public static Command SetActionWithExceptionHandling(
        this Command command,
        INitroConsole console,
        Func<ParseResult, CancellationToken, Task<int>> action)
    {
        command.SetAction(async (parseResult, cancellationToken) =>
        {
            try
            {
                return await action.Invoke(parseResult, cancellationToken);
            }
            catch (ExitException exception)
            {
                await console.Error.WriteLineAsync(exception.Message);

                return ExitCodes.Error;
            }
            catch (NitroClientAuthorizationException)
            {
                await console.Error.WriteLineAsync(
                    "The server rejected your request as unauthorized. Ensure your account or API key has the proper permissions for this action.");

                return ExitCodes.Error;
            }
            catch (NitroClientException exception)
            {
                await console.Error.WriteLineAsync(
                    $"There was an unexpected error executing your request: {exception.Message}");

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
        });

        return command;
    }

    public static Command AddGlobalNitroOptions(this Command command)
    {
        command.Options.Add(Opt<OptionalCloudUrlOption>.Instance);
        command.Options.Add(Opt<OptionalApiKeyOption>.Instance);
        command.Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        return command;
    }
}
