using ChilliCream.Nitro.CommandLine.Services.Memory;
using ChilliCream.Nitro.CommandLine.Tui.Editing;
using ChilliCream.Nitro.CommandLine.Tui.Widgets.Form;

namespace ChilliCream.Nitro.CommandLine.Tui.Memory;

/// <summary>
/// Builds deletion confirmations and permanently deletes curated memories.
/// </summary>
internal static class MemoryLifecycleActions
{
    /// <summary>
    /// Builds the confirmation dialog for permanently deleting <paramref name="record"/>.
    /// </summary>
    public static ConfirmDialog CreateForgetDialog(MemoryRecord record)
        => new(
            $"Permanently delete memory '{record.Id}'? Git history may still retain its content.",
            "Delete",
            ButtonKind.Danger);

    /// <summary>
    /// Permanently deletes the curated memory, returning a failed outcome when the
    /// store throws <see cref="ExitException"/>.
    /// </summary>
    public static async Task<MemoryForgetOutcome> ForgetAsync(
        IMemoryStore store, MemoryRecord record, CancellationToken cancellationToken)
    {
        try
        {
            await store.ForgetAsync(record.Id, cancellationToken).ConfigureAwait(false);
            return new MemoryForgetOutcome.Succeeded($"Deleted memory '{record.Id}'.");
        }
        catch (ExitException ex)
        {
            return new MemoryForgetOutcome.Failed(ex.Message);
        }
    }
}
