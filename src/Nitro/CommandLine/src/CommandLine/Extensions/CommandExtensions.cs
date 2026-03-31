using System.Net;
using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Results;

namespace ChilliCream.Nitro.CommandLine;

internal static class CommandExtensions
{
    public static Command SetActionWithExceptionHandling(
        this Command command,
        Func<ICommandServices, ParseResult, CancellationToken, Task<int>> action)
    {
        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var services = CommandExecutionContext.Services.Value!;
            var console = services.GetRequiredService<INitroConsole>();

            try
            {
                return await action.Invoke(services, parseResult, cancellationToken);
            }
            catch (ExitException exception)
            {
                console.Error.WriteErrorLine(exception.Message);
            }
            catch (NitroClientAuthorizationException)
            {
                console.Error.WriteErrorLine(
                    "The server rejected your request as unauthorized. Ensure your account or API key has the proper permissions for this action.");
            }
            catch (NitroClientHttpRequestException ex) when (ex.StatusCode == HttpStatusCode.RequestEntityTooLarge)
            {
                console.Error.WriteErrorLine(
                    "The server returned a 413 (Request Entity Too Large) HTTP status code. "
                    + "If you are running a self-hosted instance, check your ingress controller body-size limits, "
                    + "reverse proxy settings, or load balancer request size limits.");
            }
            catch (NitroClientHttpRequestException exception)
            {
                var message = exception.StatusCode is null
                    ? "The server returned an unexpected HTTP status code."
                    : $"The server returned an unexpected HTTP status code ({(int)exception.StatusCode} - {exception.StatusCode})";

                console.Error.WriteErrorLine(message);
            }
            catch (NitroClientGraphQLException exception)
            {
                var message = string.IsNullOrEmpty(exception.Code)
                    ? $"The server returned an unexpected GraphQL error: {exception.ErrorMessage}"
                    : $"The server returned an unexpected GraphQL error: {exception.ErrorMessage} ({exception.Code})";

                console.Error.WriteErrorLine(message);
            }
            catch (NitroClientException exception)
            {
                console.Error.WriteErrorLine(
                    $"There was an unexpected error: {exception.Message}");
            }
            catch (Exception ex) when (ex is OperationCanceledException or TaskCanceledException)
            {
                // No message needed for cancellation.
            }
            catch (Exception exception)
            {
                console.Error.WriteErrorLine(exception.Message);
            }

            return ExitCodes.Error;
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
