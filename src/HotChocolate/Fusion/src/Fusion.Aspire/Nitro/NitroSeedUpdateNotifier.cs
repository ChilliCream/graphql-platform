using Aspire.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HotChocolate.Fusion.Aspire.Nitro;

internal interface INitroSeedUpdateNotifier
{
    void NotifyAdopted(string message);

    void NotifyStaged(string message);
}

#pragma warning disable ASPIREINTERACTION001
internal sealed class NitroSeedUpdateNotifier(
    IInteractionService interactionService,
    IHostApplicationLifetime lifetime,
    ILogger<NitroSeedUpdateNotifier> logger)
    : INitroSeedUpdateNotifier
{
    public void NotifyAdopted(string message) => Notify(message);

    public void NotifyStaged(string message) => Notify(message);

    private void Notify(string message)
    {
        if (!interactionService.IsAvailable)
        {
            return;
        }

        _ = PromptNotificationAsync(message);
    }

    private async Task PromptNotificationAsync(string message)
    {
        try
        {
            await interactionService.PromptNotificationAsync(
                "Nitro Fusion configuration",
                message,
                new NotificationInteractionOptions { Intent = MessageIntent.Information },
                lifetime.ApplicationStopping);
        }
        catch (OperationCanceledException) when (lifetime.ApplicationStopping.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            logger.LogDebug(exception, "The Nitro Fusion configuration notification could not be shown.");
        }
    }
}
#pragma warning restore ASPIREINTERACTION001
