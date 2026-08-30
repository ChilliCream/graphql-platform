namespace ChilliCream.Nitro.CommandLine.Services.Tasks;

/// <summary>
/// The parameters for <see cref="ITaskStore.UpdateTaskAsync"/>. Each field
/// pairs a value with a "given" flag so the store can tell "not passed" apart
/// from "passed as empty", matching the CLI's per-option update semantics.
/// </summary>
internal sealed record TaskUpdate
{
    public required string Actor { get; init; }

    public string? Title { get; init; }
    public bool TitleGiven { get; init; }

    public string? Description { get; init; }
    public bool DescriptionGiven { get; init; }

    /// <summary>
    /// The normalized target status. Setting <see cref="TaskStates.Closed"/>
    /// or <see cref="TaskStates.Tombstone"/> here throws
    /// <see cref="ExitException"/>; use CloseTaskAsync, ReopenTaskAsync, or
    /// DeleteTaskAsync instead.
    /// </summary>
    public string? Status { get; init; }
    public bool StatusGiven { get; init; }

    public int? Priority { get; init; }
    public bool PriorityGiven { get; init; }

    public string? Type { get; init; }
    public bool TypeGiven { get; init; }

    public string? Assignee { get; init; }
    public bool AssigneeGiven { get; init; }

    public string? Notes { get; init; }
    public bool NotesGiven { get; init; }

    public string? Design { get; init; }
    public bool DesignGiven { get; init; }

    public string? AcceptanceCriteria { get; init; }
    public bool AcceptanceCriteriaGiven { get; init; }

    public DateTimeOffset? DueAt { get; init; }
    public bool DueAtGiven { get; init; }

    public DateTimeOffset? DeferUntil { get; init; }
    public bool DeferUntilGiven { get; init; }

    public int? EstimatedMinutes { get; init; }
    public bool EstimatedMinutesGiven { get; init; }
}
