using ChilliCream.Nitro.CommandLine.Services.Tasks;

namespace ChilliCream.Nitro.CommandLine.Tui.Details;

/// <summary>
/// One row in the loaded task's combined dependency and blocks list, indexed
/// by its position in <see cref="TaskDetailModel.Rows"/>. Null
/// <see cref="Status"/> and <see cref="Title"/> mean the target task no
/// longer exists.
/// </summary>
internal sealed record TaskDetailRow(
    int Index,
    TaskDetailRowKind Kind,
    string Type,
    string TargetId,
    string? Status,
    string? Title)
{
    /// <summary>
    /// Whether this row's dependency type gates readiness, per
    /// <see cref="TaskDependencyTypes.IsBlocking"/>.
    /// </summary>
    public bool IsBlocking => TaskDependencyTypes.IsBlocking(Type);
}
