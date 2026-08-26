using ChilliCream.Nitro.CommandLine.Services.Memory;
using ChilliCream.Nitro.CommandLine.Tui.Editing;
using ChilliCream.Nitro.CommandLine.Tui.Widgets.Form;

namespace ChilliCream.Nitro.CommandLine.Tui.Memory;

/// <summary>
/// The forget (hard delete) action for the selected curated memory: builds
/// the forget confirmation dialog and applies the delete to the memory
/// store, the same store member the CLI's <c>forget</c> command calls. No
/// equivalent action exists for a journal entry: the only write a journal
/// entry supports from the tab is <see cref="MemoryPromoteForm"/>.
/// </summary>
internal static class MemoryLifecycleActions
{
    /// <summary>
    /// Builds the confirmation dialog for permanently deleting
    /// <paramref name="record"/>, carrying the same not-a-privacy-erasure
    /// wording the CLI's confirmation prompt gives.
    /// </summary>
    public static ConfirmDialog CreateForgetDialog(MemoryRecord record)
        => new(
            $"Permanently delete memory '{record.Id}'? Git history may still retain its content.",
            "Delete",
            ButtonKind.Danger);

    /// <summary>
    /// Permanently deletes <paramref name="record"/> in its own scope.
    /// </summary>
    public static async Task<MemoryForgetOutcome> ForgetAsync(
        IMemoryStore store, MemoryRecord record, CancellationToken cancellationToken)
    {
        try
        {
            await store.ForgetAsync(record.Id, record.Scope, cancellationToken).ConfigureAwait(false);
            return new MemoryForgetOutcome.Succeeded($"Deleted memory '{record.Id}'.");
        }
        catch (ExitException ex)
        {
            return new MemoryForgetOutcome.Failed(ex.Message);
        }
    }
}
