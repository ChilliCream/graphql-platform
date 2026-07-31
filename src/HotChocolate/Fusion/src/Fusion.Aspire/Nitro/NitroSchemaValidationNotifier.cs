using Aspire.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HotChocolate.Fusion.Aspire.Nitro;

internal interface INitroSchemaValidationNotifier
{
    void NotifyViolations(string message);

    void NotifyRestored(string message);
}

#pragma warning disable ASPIREINTERACTION001
internal sealed class NitroSchemaValidationNotifier(
    IInteractionService interactionService,
    IHostApplicationLifetime lifetime,
    ILogger<NitroSchemaValidationNotifier> logger)
    : INitroSchemaValidationNotifier
{
    public void NotifyViolations(string message)
        => Notify(MessageIntent.Error, message);

    public void NotifyRestored(string message)
        => Notify(MessageIntent.Success, message);

    private void Notify(MessageIntent intent, string message)
    {
        if (!interactionService.IsAvailable)
        {
            return;
        }

        _ = PromptNotificationAsync(intent, message);
    }

    private async Task PromptNotificationAsync(MessageIntent intent, string message)
    {
        try
        {
            await interactionService.PromptNotificationAsync(
                "Nitro schema validation",
                message,
                new NotificationInteractionOptions { Intent = intent },
                lifetime.ApplicationStopping);
        }
        catch (OperationCanceledException) when (lifetime.ApplicationStopping.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            logger.LogDebug(exception, "The Nitro schema validation notification could not be shown.");
        }
    }
}
#pragma warning restore ASPIREINTERACTION001
