using System.Net;
using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;

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
            var context = services.GetRequiredService<NitroClientContext>();

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
            catch (NitroClientNotFoundException ex)
            {
                console.Error.WriteErrorLine(
                    ex.Message
                    + Environment.NewLine
                    + "This may mean the entity does not exist, or that you do not have permission to view it."
                    + Environment.NewLine
                    + $"If you are targeting a dedicated or self-hosted instance, make sure you supply the correct '{OptionalCloudUrlOption.OptionName}'. "
                    + $"Currently targeting '{GetCleanApiUrl(context.Url)}'.");
            }
            catch (NitroClientAuthorizationException)
            {
                console.Error.WriteErrorLine(
                    "The server rejected your request as unauthorized."
                    + Environment.NewLine
                    + "Ensure your account or API key has the proper permissions for this action."
                    + Environment.NewLine
                    + $"If you are targeting a dedicated or self-hosted instance, make sure you supply the correct '{OptionalCloudUrlOption.OptionName}'. "
                    + $"Currently targeting '{GetCleanApiUrl(context.Url)}'.");
            }
            catch (NitroClientHttpRequestException ex) when (ex.StatusCode == HttpStatusCode.RequestEntityTooLarge)
            {
                console.Error.WriteErrorLine(
                    "The server returned a 413 (Request Entity Too Large) HTTP status code."
                    + Environment.NewLine
                    + "If you are targeting a self-hosted instance, check your ingress controller, reverse proxy, or load balancer request size limits.");
            }
            catch (NitroClientHttpRequestException ex)
            {
                var message = ex.StatusCode is null
                    ? "The server returned an unexpected HTTP status code."
                    : $"The server returned an unexpected HTTP status code ({(int)ex.StatusCode} - {ex.StatusCode})";

                console.Error.WriteErrorLine(message);
            }
            catch (NitroClientGraphQLException ex) when (ex.Code == "HC0020")
            {
                console.Error.WriteErrorLine(
                    "The server rejected the persisted operation of the command."
                    + Environment.NewLine
                    + "If you are targeting a self-hosted instance, make sure it's running the latest version, or downgrade your CLI to match.");
            }
            catch (NitroClientGraphQLException ex)
            {
                var message = string.IsNullOrEmpty(ex.Code)
                    ? $"The server returned an unexpected GraphQL error: {ex.ErrorMessage.EscapeMarkup()}"
                    : $"The server returned an unexpected GraphQL error: {ex.ErrorMessage.EscapeMarkup()} ({ex.Code})";

                console.Error.WriteErrorLine(message);
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

    private static string GetCleanApiUrl(Uri apiUrl)
    {
        var uriBuilder = new UriBuilder(apiUrl.AbsoluteUri)
        {
            Path = null,
            Query = string.Empty,
            Fragment = string.Empty,
            UserName = string.Empty,
            Password = string.Empty
        };

        return uriBuilder.Uri.AbsoluteUri.TrimEnd('/');
    }
}
