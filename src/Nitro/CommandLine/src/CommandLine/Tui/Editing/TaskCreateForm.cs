using System.Globalization;
using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Tui.Input;
using ChilliCream.Nitro.CommandLine.Tui.Widgets.Form;
using Spectre.Console.Rendering;
using Form = ChilliCream.Nitro.CommandLine.Tui.Widgets.Form.Form;

namespace ChilliCream.Nitro.CommandLine.Tui.Editing;

/// <summary>
/// Collects a task title, type, priority, labels, and description for creation.
/// When supplied a parent id, includes a selector to retain that parent or
/// create a root task.
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
    /// The footer hints shown while the create form captures input.
    /// </summary>
    public static readonly IReadOnlyList<KeyHint> Hints =
    [
        new KeyHint("tab", "next field"),
        new KeyHint("ctrl+s", "create"),
        new KeyHint("esc", "cancel")
    ];

    private const string DefaultPriorityId = "2";

    private static readonly SelectOption[] s_wellKnownTypes =
    [
        new(TaskTypes.Task, "Task"),
        new(TaskTypes.Bug, "Bug"),
        new(TaskTypes.Feature, "Feature"),
        new(TaskTypes.Epic, "Epic"),
        new(TaskTypes.Chore, "Chore"),
        new(TaskTypes.Docs, "Docs"),
        new(TaskTypes.Question, "Question")
    ];

    private static readonly SelectOption[] s_wellKnownPriorities =
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
            s_wellKnownTypes,
            initialSelectedId: typePreset);

        // A supplied parent can be retained or cleared by the parent selector.
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
            s_wellKnownPriorities,
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
    /// Whether any field differs from its initial value, including the supplied
    /// type and parent defaults.
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
    /// Creates a task from submitted values, retaining or clearing the supplied
    /// parent according to the parent selector. Returns a failed outcome for a
    /// store rejection represented by <see cref="ExitException"/>.
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
