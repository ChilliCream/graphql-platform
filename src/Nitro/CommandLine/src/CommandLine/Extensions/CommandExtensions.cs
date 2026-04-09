using System.Net;
using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.CommandLine.Helpers;
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
            var services = CommandExecutionContext.s_services.Value!;
            var console = services.GetRequiredService<INitroConsole>();

            try
            {
                return await action.Invoke(services, parseResult, cancellationToken);
            }
            catch (ExitException exception)
            {
                if (!string.IsNullOrEmpty(exception.Message))
                {
                    console.Error.WriteErrorLine(exception.Message);
                }
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
            catch (NitroClientHttpRequestException ex)
            {
                var message = ex.StatusCode is null
                    ? "The server returned an unexpected HTTP status code."
                    : $"The server returned an unexpected HTTP status code ({(int)ex.StatusCode} - {ex.StatusCode})";

                console.Error.WriteErrorLine(message);
            }
            catch (NitroClientGraphQLException ex) when (ex.Code == "HC0067")
            {
                console.Error.WriteErrorLine(
                    "The server rejected the persisted operation of the command. Make sure your Nitro backend is on the latest version.");
            }
            catch (NitroClientGraphQLException ex)
            {
                var message = string.IsNullOrEmpty(ex.Code)
                    ? $"The server returned an unexpected GraphQL error: {ex.ErrorMessage.EscapeMarkup()}"
                    : $"The server returned an unexpected GraphQL error: {ex.ErrorMessage.EscapeMarkup()} ({ex.Code})";

                console.Error.WriteErrorLine(message);
            }
            catch (NitroClientException ex)
            {
                console.Error.WriteErrorLine(
                    $"There was an unexpected client error: {ex.Message.EscapeMarkup()}");
            }
            catch (Exception ex) when (ex is OperationCanceledException or TaskCanceledException)
            {
                // No message needed for cancellation.
            }
            catch (Exception ex)
            {
                console.Error.WriteErrorLine($"There was an unexpected error: {ex.Message.EscapeMarkup()}");
            }

            return ExitCodes.Error;
        });

        return command;
    }

    public static Command AddExamples(this Command command, params string[] examples)
    {
        CommandExamples.AddExamples(command, examples);
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
