using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Tui.Shell;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Shell;

/// <summary>
/// Builds <see cref="TaskItem"/> instances with sensible defaults for
/// <see cref="TuiShell"/> wiring tests.
/// </summary>
internal static class TaskItemBuilder
{
    public static TaskItem Create(string id, string title = "", string status = TaskStates.Open)
        => new()
        {
            Id = id,
            Title = title.Length == 0 ? id : title,
            Status = status,
            CreatedAt = DateTimeOffset.UnixEpoch,
            UpdatedAt = DateTimeOffset.UnixEpoch
        };
}
