using System.Globalization;
using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Tui.Input;
using ChilliCream.Nitro.CommandLine.Tui.Widgets.Form;
using Spectre.Console.Rendering;
using Form = ChilliCream.Nitro.CommandLine.Tui.Widgets.Form.Form;

namespace ChilliCream.Nitro.CommandLine.Tui.Editing;

/// <summary>
/// The outcome of submitting a <see cref="TaskCreateForm"/> against the task
/// store.
/// </summary>
internal abstract record TaskCreateOutcome
{
    private TaskCreateOutcome()
    {
    }

    /// <summary>
    /// The task was created, carrying the id the store allocated for it.
    /// </summary>
    public sealed record Succeeded(string TaskId, string ToastText) : TaskCreateOutcome;

    /// <summary>
    /// The store rejected the write, carrying its <see cref="ExitException"/> message.
    /// </summary>
    public sealed record Failed(string ToastText) : TaskCreateOutcome;

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
/// The task create form: title, type, priority, labels, and description.
/// Status is always open on create and is not a field; due, defer, and
/// estimate are not fields either. Built with an optional parent task id: when
/// given, the form gains a parent field defaulted to that parent, letting the
/// user switch it to create a root task instead. Submitting passes the
/// resulting parent id (or <see langword="null"/>) through to
/// <see cref="ITaskStore.CreateTaskAsync"/>, the same as the CLI's
/// <c>task create --parent</c> when a parent is kept. The host is expected to
/// feed it raw key input via <see cref="HandleKey"/> and call
/// <see cref="SubmitAsync"/> once it returns <see cref="FormResult.Submitted"/>
/// on the primary button.
/// </summary>
internal sealed class TaskCreateForm
{
    public const string TitleFieldId = "title";
    public const string TypeFieldId = "type";
    public const string ParentFieldId = "parent";
    public const string PriorityFieldId = "priority";
    public const string LabelsFieldId = "labels";
    public const string DescriptionFieldId = "description";

    public const string CreateButtonId = "create";
    public const string CancelButtonId = "cancel";

    /// <summary>
    /// The <see cref="ParentFieldId"/> option that keeps the parent this form
    /// was built with.
    /// </summary>
    public const string ChildParentOptionId = "child";

    /// <summary>
    /// The <see cref="ParentFieldId"/> option that clears the parent this
    /// form was built with, creating a root task instead.
    /// </summary>
    public const string NoParentOptionId = "none";

    /// <summary>
    /// The footer hints for the task create form: its keys are consumed
    /// entirely while it is active, so no global hints follow.
    /// </summary>
    public static readonly IReadOnlyList<KeyHint> Hints =
    [
        new KeyHint("tab", "next field"),
        new KeyHint("enter", "create"),
        new KeyHint("esc", "cancel")
    ];

    private const string DefaultPriorityId = "2";

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

    private static readonly SelectOption[] WellKnownPriorities =
    [
        new("0", TaskPriorities.Format(0)),
        new("1", TaskPriorities.Format(1)),
        new("2", TaskPriorities.Format(2)),
        new("3", TaskPriorities.Format(3)),
        new("4", TaskPriorities.Format(4))
    ];

    private readonly string _typePreset;
    private readonly string? _parentId;
    private readonly Form _form;
    private readonly TextField _titleField;
    private readonly SelectField _typeField;
    private readonly SelectField? _parentField;
    private readonly SelectField _priorityField;
    private readonly EditableListField _labelsField;
    private readonly TextAreaField _descriptionField;

    public TaskCreateForm(string typePreset, string? parentId = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(typePreset);

        _typePreset = typePreset;
        _parentId = parentId;

        _titleField = new TextField(
            TitleFieldId,
            "Title",
            required: true,
            validator: RequireNonBlankTitle);

        _typeField = new SelectField(
            TypeFieldId,
            "Type",
            WellKnownTypes,
            initialSelectedId: typePreset);

        // A selected board row becomes the new task's parent by default, but
        // this field lets the user clear it and create a root task instead:
        // without it, a populated board column (which always has a
        // selection) could never create a top-level task from the board.
        _parentField = parentId is null
            ? null
            : new SelectField(
                ParentFieldId,
                "Parent",
                [
                    new SelectOption(ChildParentOptionId, $"Child of '{parentId}'"),
                    new SelectOption(NoParentOptionId, "No parent (top-level)")
                ],
                initialSelectedId: ChildParentOptionId);

        _priorityField = new SelectField(
            PriorityFieldId,
            "Priority",
            WellKnownPriorities,
            initialSelectedId: DefaultPriorityId);

        _labelsField = new EditableListField(LabelsFieldId, "Labels");
        _descriptionField = new TextAreaField(DescriptionFieldId, "Description");

        var buttons = new FormButtons(
        [
            new FormButtonSpec(CreateButtonId, "Create", ButtonKind.Primary),
            new FormButtonSpec(CancelButtonId, "Cancel", ButtonKind.Secondary)
        ]);

        IReadOnlyList<FormField> fields = _parentField is null
            ? [_titleField, _typeField, _priorityField, _labelsField, _descriptionField]
            : [_titleField, _typeField, _parentField, _priorityField, _labelsField, _descriptionField];

        _form = new Form(Title(typePreset, parentId), fields, buttons);
    }

    /// <summary>
    /// Whether any field's current value differs from its blank default: gates
    /// whether Esc should ask for discard confirmation before cancelling.
    /// </summary>
    public bool IsDirty
        => Text(_titleField).Length != 0
        || Text(_typeField) != _typePreset
        || (_parentField is not null && Text(_parentField) != ChildParentOptionId)
        || Text(_priorityField) != DefaultPriorityId
        || Text(_descriptionField).Length != 0
        || List(_labelsField).Count != 0;

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
    /// Creates the task from the submitted <paramref name="values"/> through
    /// the task store, carrying the parent id this form was built with, if
    /// any.
    /// </summary>
    public async Task<TaskCreateOutcome> SubmitAsync(
        ITaskStore store,
        IReadOnlyDictionary<string, FormValue> values,
        string actor,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(values);
        ArgumentException.ThrowIfNullOrEmpty(actor);

        var title = Text(values, TitleFieldId);
        var type = Text(values, TypeFieldId);
        var priority = int.Parse(Text(values, PriorityFieldId), CultureInfo.InvariantCulture);
        var labels = List(values, LabelsFieldId);
        var description = Text(values, DescriptionFieldId);
        var parentId = _parentField is null
            ? _parentId
            : Text(values, ParentFieldId) == NoParentOptionId ? null : _parentId;

        try
        {
            var result = await store.CreateTaskAsync(
                new TaskCreation
                {
                    Title = title,
                    Description = description,
                    Priority = priority,
                    Type = type,
                    Labels = labels,
                    ParentId = parentId,
                    Actor = actor
                },
                cancellationToken);

            return new TaskCreateOutcome.Succeeded(result.Id, $"Created task '{result.Id}'.");
        }
        catch (ExitException ex)
        {
            return new TaskCreateOutcome.Failed(ex.Message);
        }
    }

    private static string? RequireNonBlankTitle(FormValue value)
        => value is FormValue.Text { Value: var text } && text.Trim().Length == 0
            ? "Title is required."
            : null;

    private static string Title(string typePreset, string? parentId)
    {
        var typeLabel = typePreset.Length == 0
            ? typePreset
            : char.ToUpperInvariant(typePreset[0]) + typePreset[1..];

        return parentId is null
            ? $"Create {typeLabel}"
            : $"Create {typeLabel} (child of '{parentId}')";
    }

    private static string Text(FormField field)
        => field.GetValue() is FormValue.Text { Value: var value } ? value : "";

    private static IReadOnlyList<string> List(FormField field)
        => field.GetValue() is FormValue.List { Values: var values } ? values : [];

    private static string Text(IReadOnlyDictionary<string, FormValue> values, string id)
        => values[id] is FormValue.Text { Value: var value } ? value : "";

    private static IReadOnlyList<string> List(IReadOnlyDictionary<string, FormValue> values, string id)
        => values[id] is FormValue.List { Values: var value } ? value : [];
}
