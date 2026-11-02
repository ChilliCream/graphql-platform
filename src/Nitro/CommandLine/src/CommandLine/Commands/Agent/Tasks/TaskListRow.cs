using ChilliCream.Nitro.CommandLine.Services.Tasks;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Tasks;

/// <summary>
/// The subset of a task's columns needed to print one list row.
/// </summary>
internal sealed class TaskListRow
{
    public required string Id { get; init; }
    public required int Priority { get; init; }
    public required string Type { get; init; }
    public required string Status { get; init; }
    public required string Title { get; init; }

    public string Format()
        => $"{Id}  {TaskPriorities.Format(Priority)}  {Type}  {Status}  {Title}";
}
