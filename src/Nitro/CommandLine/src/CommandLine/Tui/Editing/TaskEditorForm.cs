using System.Globalization;
using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Tui.Input;
using ChilliCream.Nitro.CommandLine.Tui.Widgets.Form;
using Spectre.Console.Rendering;
using Form = ChilliCream.Nitro.CommandLine.Tui.Widgets.Form.Form;

namespace ChilliCream.Nitro.CommandLine.Tui.Editing;

/// <summary>
/// The task field values a <see cref="TaskEditorForm"/> was built from, used to
/// diff the submitted values against what the store last had.
/// </summary>
internal sealed record TaskEditorSnapshot(
    string Title,
    string Status,
    int Priority,
    string Type,
    IReadOnlyList<string> Labels,
    string Description,
    string Notes)
{
    /// <summary>
    /// Captures a snapshot from a task and its labels, normalizing
    /// <paramref name="task"/>'s description and notes line endings to match
    /// what <see cref="TaskEditorForm"/>'s text areas will echo back
    /// unmodified.
    /// </summary>
    public static TaskEditorSnapshot FromTask(TaskItem task, IReadOnlyList<string> labels)
        => new(
            task.Title,
            task.Status,
            task.Priority,
            task.Type,
            labels,
            NormalizeLineEndings(task.Description),
            NormalizeLineEndings(task.Notes));

    private static string NormalizeLineEndings(string value)
        => value.Contains('\r') ? value.Replace("\r\n", "\n").Replace('\r', '\n') : value;
}

/// <summary>
/// The outcome of submitting a <see cref="TaskEditorForm"/> against the task
/// store.
/// </summary>
internal abstract record TaskEditorOutcome
{
    private TaskEditorOutcome()
    {
    }

    /// <summary>
    /// Every field that differed from the snapshot was written. Carries the
    /// changed store field names, empty when no field differed.
    /// </summary>
    public sealed record Succeeded(IReadOnlyList<string> ChangedFields, string ToastText) : TaskEditorOutcome;

    /// <summary>
    /// The store rejected a write, carrying its <see cref="ExitException"/> message.
    /// </summary>
    public sealed record Failed(string ToastText) : TaskEditorOutcome;

    /// <summary>
    /// The shell toast this outcome should show: success styled for
    /// <see cref="Succeeded"/>, error styled for <see cref="Failed"/>.
    /// </summary>
    public TuiMessage.ShowToast ToShowToast() => this switch
    {
        Succeeded succeeded => new TuiMessage.ShowToast(succeeded.ToastText, ToastStyle.Success),
        Failed failed => new TuiMessage.ShowToast(failed.ToastText, ToastStyle.Error),
        _ => throw new NotSupportedException()
    };
}

/// <summary>
/// The task edit form: title, status, priority, type, labels, description,
/// and notes, pre-populated from a task and its labels. Submitting computes
/// the diff against the loaded snapshot and writes only the changed fields
/// through the task store; label changes are applied as adds and removes
/// against the unchanged set. The host is expected to feed it raw key input
/// via <see cref="HandleKey"/> and call <see cref="SubmitAsync"/> once it
/// returns <see cref="FormResult.Submitted"/> on the primary button.
/// </summary>
internal sealed class TaskEditorForm
{
    public const string TitleFieldId = "title";
    public const string StatusFieldId = "status";
    public const string PriorityFieldId = "priority";
    public const string TypeFieldId = "type";
    public const string LabelsFieldId = "labels";
    public const string DescriptionFieldId = "description";
    public const string NotesFieldId = "notes";

    public const string SaveButtonId = "save";
    public const string CancelButtonId = "cancel";

    private static readonly SelectOption[] WellKnownStatuses =
    [
        new(TaskStates.Open, "Open"),
        new(TaskStates.InProgress, "In Progress"),
        new(TaskStates.Blocked, "Blocked"),
        new(TaskStates.Deferred, "Deferred")
    ];

    private static readonly SelectOption[] WellKnownPriorities =
    [
        new("0", TaskPriorities.Format(0)),
        new("1", TaskPriorities.Format(1)),
        new("2", TaskPriorities.Format(2)),
        new("3", TaskPriorities.Format(3)),
        new("4", TaskPriorities.Format(4))
    ];

    private static readonly SelectOption[] WellKnownTypes =
    [
        new(TaskTypes.Task, "Task"),
        new(TaskTypes.Bug, "Bug"),
        new(TaskTypes.Feature, "Feature"),
        new(TaskTypes.Epic, "Epic"),
        new(TaskTypes.Chore, "Chore"),
        new(TaskTypes.Docs, "Docs"),
        new(TaskTypes.Question, "Question")
    ];

    private readonly string _taskId;
    private readonly TaskEditorSnapshot _snapshot;
    private readonly Form _form;
    private readonly TextField _titleField;
    private readonly SelectField _statusField;
    private readonly SelectField _priorityField;
    private readonly SelectField _typeField;
    private readonly EditableListField _labelsField;
    private readonly TextAreaField _descriptionField;
    private readonly TextAreaField _notesField;

    public TaskEditorForm(TaskItem task, IReadOnlyList<string> labels)
    {
        ArgumentNullException.ThrowIfNull(task);
        ArgumentNullException.ThrowIfNull(labels);

        _taskId = task.Id;
        _snapshot = TaskEditorSnapshot.FromTask(task, labels);

        _titleField = new TextField(
            TitleFieldId,
            "Title",
            required: true,
            initialValue: _snapshot.Title,
            validator: RequireNonEmptyTitle);

        _statusField = new SelectField(
            StatusFieldId,
            "Status",
            WithCurrentOption(WellKnownStatuses, _snapshot.Status),
            initialSelectedId: _snapshot.Status);

        _priorityField = new SelectField(
            PriorityFieldId,
            "Priority",
            WithCurrentOption(WellKnownPriorities, _snapshot.Priority.ToString(CultureInfo.InvariantCulture)),
            initialSelectedId: _snapshot.Priority.ToString(CultureInfo.InvariantCulture));

        _typeField = new SelectField(
            TypeFieldId,
            "Type",
            WithCurrentOption(WellKnownTypes, _snapshot.Type),
            initialSelectedId: _snapshot.Type);

        _labelsField = new EditableListField(LabelsFieldId, "Labels", initialValues: _snapshot.Labels);
        _descriptionField = new TextAreaField(DescriptionFieldId, "Description", initialValue: _snapshot.Description);
        _notesField = new TextAreaField(NotesFieldId, "Notes", initialValue: _snapshot.Notes);

        var buttons = new FormButtons(
        [
            new FormButtonSpec(SaveButtonId, "Save", ButtonKind.Primary),
            new FormButtonSpec(CancelButtonId, "Cancel", ButtonKind.Secondary)
        ]);

        _form = new Form(
            $"Edit Task '{task.Id}'",
            [_titleField, _statusField, _priorityField, _typeField, _labelsField, _descriptionField, _notesField],
            buttons);
    }

    /// <summary>
    /// Whether any field's current value differs from the loaded snapshot:
    /// gates whether Esc should ask for discard confirmation before
    /// cancelling.
    /// </summary>
    public bool IsDirty
        => Text(_titleField) != _snapshot.Title
        || Text(_statusField) != _snapshot.Status
        || int.Parse(Text(_priorityField), CultureInfo.InvariantCulture) != _snapshot.Priority
        || Text(_typeField) != _snapshot.Type
        || Text(_descriptionField) != _snapshot.Description
        || Text(_notesField) != _snapshot.Notes
        || LabelsChanged(_snapshot.Labels, List(_labelsField));

    /// <summary>
    /// The field currently holding focus, or <see langword="null"/> when
    /// focus is on the button row.
    /// </summary>
    public FormField? FocusedField => _form.FocusedField;

    /// <summary>
    /// Handles one raw key. Returns <see langword="null"/> while the form is
    /// still active, or the terminal <see cref="FormResult"/> once the
    /// interaction ends.
    /// </summary>
    public FormResult? HandleKey(ConsoleKeyInfo info) => _form.HandleKey(info);

    /// <summary>
    /// Renders the form as a titled, rounded panel centered within the given
    /// area.
    /// </summary>
    public IRenderable Render(int width, int height) => _form.Render(width, height);

    /// <summary>
    /// Applies the diff between <paramref name="values"/> (from a
    /// <see cref="FormResult.Submitted"/>) and the loaded snapshot to the task
    /// store: an <see cref="ITaskStore.UpdateTaskAsync"/> call carrying only
    /// the scalar fields that changed, plus label adds and removes for the
    /// labels that changed. Writes nothing when no field differs.
    /// </summary>
    public async Task<TaskEditorOutcome> SubmitAsync(
        ITaskStore store,
        IReadOnlyDictionary<string, FormValue> values,
        string actor,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(values);
        ArgumentException.ThrowIfNullOrEmpty(actor);

        var title = Text(values, TitleFieldId);
        var status = Text(values, StatusFieldId);
        var priority = int.Parse(Text(values, PriorityFieldId), CultureInfo.InvariantCulture);
        var type = Text(values, TypeFieldId);
        var description = Text(values, DescriptionFieldId);
        var notes = Text(values, NotesFieldId);
        var labels = List(values, LabelsFieldId);

        try
        {
            var changedFields = new List<string>();

            var titleGiven = title != _snapshot.Title;
            var statusGiven = status != _snapshot.Status;
            var priorityGiven = priority != _snapshot.Priority;
            var typeGiven = type != _snapshot.Type;
            var descriptionGiven = description != _snapshot.Description;
            var notesGiven = notes != _snapshot.Notes;

            if (titleGiven || statusGiven || priorityGiven || typeGiven || descriptionGiven || notesGiven)
            {
                var update = new TaskUpdate
                {
                    Actor = actor,
                    Title = title,
                    TitleGiven = titleGiven,
                    Status = status,
                    StatusGiven = statusGiven,
                    Priority = priority,
                    PriorityGiven = priorityGiven,
                    Type = type,
                    TypeGiven = typeGiven,
                    Description = description,
                    DescriptionGiven = descriptionGiven,
                    Notes = notes,
                    NotesGiven = notesGiven
                };

                var result = await store.UpdateTaskAsync(_taskId, update, cancellationToken);
                changedFields.AddRange(result.ChangedFields);
            }

            var (added, removed) = DiffLabels(_snapshot.Labels, labels);

            if (added.Count > 0)
            {
                await store.AddLabelAsync(_taskId, added, actor, cancellationToken);
                changedFields.Add("labels");
            }

            foreach (var label in removed)
            {
                await store.RemoveLabelAsync(_taskId, label, actor, cancellationToken);

                if (!changedFields.Contains("labels"))
                {
                    changedFields.Add("labels");
                }
            }

            var toastText = changedFields.Count == 0
                ? $"No changes to task '{_taskId}'."
                : $"Updated task '{_taskId}'.";

            return new TaskEditorOutcome.Succeeded(changedFields, toastText);
        }
        catch (ExitException ex)
        {
            return new TaskEditorOutcome.Failed(ex.Message);
        }
    }

    private static string? RequireNonEmptyTitle(FormValue value)
        => value is FormValue.Text { Value: var text } && text.Trim().Length == 0
            ? "Title is required."
            : null;

    /// <summary>
    /// Returns <paramref name="wellKnown"/> as-is when it already contains
    /// <paramref name="currentId"/>, otherwise appends it: keeps a task whose
    /// status, priority, or type is a custom or terminal value round-tripping
    /// as its own selected option instead of silently defaulting to the first
    /// well-known choice.
    /// </summary>
    private static IReadOnlyList<SelectOption> WithCurrentOption(
        IReadOnlyList<SelectOption> wellKnown, string currentId)
    {
        foreach (var option in wellKnown)
        {
            if (option.Id == currentId)
            {
                return wellKnown;
            }
        }

        return [.. wellKnown, new SelectOption(currentId, FormatFallbackLabel(currentId))];
    }

    private static string FormatFallbackLabel(string id)
        => id.Length == 0 ? id : char.ToUpperInvariant(id[0]) + id[1..].Replace('_', ' ');

    private static bool LabelsChanged(IReadOnlyList<string> before, IReadOnlyList<string> after)
    {
        var (added, removed) = DiffLabels(before, after);
        return added.Count > 0 || removed.Count > 0;
    }

    /// <summary>
    /// Computes the labels to add and remove to turn <paramref name="before"/>
    /// into <paramref name="after"/>, comparing trimmed, lowercased values to
    /// match the store's own label normalization.
    /// </summary>
    private static (IReadOnlyList<string> Added, IReadOnlyList<string> Removed) DiffLabels(
        IReadOnlyList<string> before, IReadOnlyList<string> after)
    {
        var normalizedBefore = before.Select(NormalizeLabel).ToHashSet();
        var normalizedAfter = after.Select(NormalizeLabel).ToHashSet();

        var added = normalizedAfter.Where(label => !normalizedBefore.Contains(label)).ToList();
        var removed = normalizedBefore.Where(label => !normalizedAfter.Contains(label)).ToList();

        return (added, removed);
    }

    private static string NormalizeLabel(string label) => label.Trim().ToLowerInvariant();

    private static string Text(FormField field)
        => field.GetValue() is FormValue.Text { Value: var value } ? value : "";

    private static IReadOnlyList<string> List(FormField field)
        => field.GetValue() is FormValue.List { Values: var values } ? values : [];

    private static string Text(IReadOnlyDictionary<string, FormValue> values, string id)
        => values[id] is FormValue.Text { Value: var value } ? value : "";

    private static IReadOnlyList<string> List(IReadOnlyDictionary<string, FormValue> values, string id)
        => values[id] is FormValue.List { Values: var value } ? value : [];
}
