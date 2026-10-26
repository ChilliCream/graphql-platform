namespace ChilliCream.Nitro.CommandLine.Services.Tasks;

/// <summary>
/// Task field changes, applied only when their corresponding Given flags are set.
/// </summary>
internal sealed record TaskUpdate
{
    public required string Actor { get; init; }

    public string? Title { get; init; }
    public bool TitleGiven { get; init; }

    public string? Description { get; init; }
    public bool DescriptionGiven { get; init; }

    /// <summary>
    /// The target status, normalized when <see cref="StatusGiven"/> is true.
    /// Updates cannot set closed, archived, or tombstone status, or change the status
    /// of an already closed or archived task.
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
