using Npgsql;

namespace Mocha.Sagas.Tests;

public static class TestExtensions
{
    /// <summary>
    /// Handles an event with the saga and then dispatches the messages that the saga produced, as the saga
    /// consumer does after it commits the saga transaction.
    /// </summary>
    public static async Task<bool> HandleEventAndDispatchAsync(this Saga saga, IConsumeContext context)
    {
        var pendingDispatches = context.GetSagaFeature().PendingDispatches;
        pendingDispatches.Clear();

        var handled = await saga.HandleEvent(context);

        var bus = context.GetBus();
        foreach (var dispatch in pendingDispatches)
        {
            await dispatch(bus, context.CancellationToken);
        }

        return handled;
    }

    // TODO: MessageBusMigrator doesn't exist in this codebase - needs to be adapted or removed
    public static async Task MigrateAsync(this NpgsqlConnection connection)
    {
        // Stubbed out - MessageBusMigrator not available
        await Task.CompletedTask;
    }
}
