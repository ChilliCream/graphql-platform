using Aspire.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HotChocolate.Fusion.Aspire.Nitro;

internal interface INitroSchemaValidationNotifier
{
    void NotifyViolations(string gatewayName, string message);

    void NotifyRestored(string message);
}

internal interface INitroCompositionNotifier
{
    void NotifyFailure(string gatewayName, string message);
}

internal sealed class NitroSchemaValidationNotifier(
    IInteractionService interactionService,
    IHostApplicationLifetime lifetime,
    ILogger<NitroSchemaValidationNotifier> logger)
    : INitroSchemaValidationNotifier
    , INitroCompositionNotifier
{
    public void NotifyViolations(string gatewayName, string message)
        => Notify(MessageIntent.Error, message, gatewayName);

    public void NotifyRestored(string message)
        => Notify(MessageIntent.Success, message, null);

    public void NotifyFailure(string gatewayName, string message)
        => Notify(MessageIntent.Error, message, gatewayName);

    private void Notify(MessageIntent intent, string message, string? gatewayName)
    {
        if (!interactionService.IsAvailable)
        {
            return;
        }

        _ = PromptNotificationAsync(intent, message, gatewayName);
    }

    private async Task PromptNotificationAsync(
        MessageIntent intent,
        string message,
        string? gatewayName)
    {
        try
        {
            var options = new NotificationInteractionOptions { Intent = intent };
            if (gatewayName is not null)
            {
                options.LinkText = "View logs";
                options.LinkUrl = $"/consolelogs/resource/{Uri.EscapeDataString(gatewayName)}";
            }

            await interactionService.PromptNotificationAsync(
                "Nitro:",
                message,
                options,
                lifetime.ApplicationStopping);
        }
        catch (OperationCanceledException) when (lifetime.ApplicationStopping.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            logger.LogDebug(exception, "The Nitro notification could not be shown.");
        }
    }
}
