using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Tui.Editing;
using ChilliCream.Nitro.CommandLine.Tui.Input;
using ChilliCream.Nitro.CommandLine.Tui.Widgets.Form;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Editing;

public sealed class TaskEditorFormTests
{
    private static ConsoleKeyInfo Key(char c) => new(c, ConsoleKey.NoName, false, false, false);

    private static ConsoleKeyInfo Key(ConsoleKey key) => new('\0', key, false, false, false);

    private static void Type(TaskEditorForm form, string text)
    {
        foreach (var c in text)
        {
            form.HandleKey(Key(c));
        }
    }

    private static void TabTo(TaskEditorForm form, int times)
    {
        for (var i = 0; i < times; i++)
        {
            form.HandleKey(Key(ConsoleKey.Tab));
        }
    }

    private static FormResult.Submitted Save(TaskEditorForm form)
    {
        // Tab from wherever focus currently sits until it reaches the button
        // row (FocusedField null), then activate the default Save button.
        while (form.FocusedField is not null)
        {
            form.HandleKey(Key(ConsoleKey.Tab));
        }

        return Assert.IsType<FormResult.Submitted>(form.HandleKey(Key(ConsoleKey.Enter)));
    }

    [Fact]
    public void Constructor_Should_PrepopulateEveryField_FromTaskAndLabels()
    {
        // arrange
        var task = TaskItemBuilder.Create(
            "a1",
            "Title",
            TaskStates.InProgress,
            priority: 1,
            type: TaskTypes.Bug,
            description: "desc",
            notes: "notes");

        // act
        var form = new TaskEditorForm(task, ["alpha", "beta"]);

        // assert
        Assert.Equal(new FormValue.Text("Title"), form.FocusedField?.GetValue());

        TabTo(form, 1);
        Assert.Equal(new FormValue.Text(TaskStates.InProgress), form.FocusedField?.GetValue());

        TabTo(form, 1);
        Assert.Equal(new FormValue.Text("1"), form.FocusedField?.GetValue());

        TabTo(form, 1);
        Assert.Equal(new FormValue.Text(TaskTypes.Bug), form.FocusedField?.GetValue());

        TabTo(form, 1);
        Assert.Equal(new FormValue.List(["alpha", "beta"]), form.FocusedField?.GetValue());

        TabTo(form, 1);
        Assert.Equal(new FormValue.Text("desc"), form.FocusedField?.GetValue());

        TabTo(form, 1);
        Assert.Equal(new FormValue.Text("notes"), form.FocusedField?.GetValue());
    }

    [Fact]
    public void Constructor_Should_NormalizeCrlf_InDescriptionAndNotes()
    {
        // arrange
        var task = TaskItemBuilder.Create(
            "a1", description: "line1\r\nline2", notes: "n1\r\nn2");

        // act
        var form = new TaskEditorForm(task, []);

        // assert
        TabTo(form, 5);
        Assert.Equal(new FormValue.Text("line1\nline2"), form.FocusedField?.GetValue());

        TabTo(form, 1);
        Assert.Equal(new FormValue.Text("n1\nn2"), form.FocusedField?.GetValue());
    }

    [Fact]
    public void Constructor_Should_RoundTripCustomStatus_When_NotAmongWellKnownOptions()
    {
        // arrange: a closed task opened for editing keeps its own status
        // selected instead of silently defaulting to "open".
        var task = TaskItemBuilder.Create("a1", status: TaskStates.Closed);

        // act
        var form = new TaskEditorForm(task, []);

        // assert
        TabTo(form, 1);
        Assert.Equal(new FormValue.Text(TaskStates.Closed), form.FocusedField?.GetValue());
        Assert.False(form.IsDirty);
    }

    [Fact]
    public void IsDirty_Should_BeFalse_Initially()
    {
        // arrange
        var task = TaskItemBuilder.Create("a1", "Title");

        // act
        var form = new TaskEditorForm(task, ["x"]);

        // assert
        Assert.False(form.IsDirty);
    }

    [Fact]
    public void IsDirty_Should_BeTrue_When_TitleEdited()
    {
        // arrange
        var task = TaskItemBuilder.Create("a1", "Title");
        var form = new TaskEditorForm(task, []);

        // act
        form.HandleKey(Key('!'));

        // assert
        Assert.True(form.IsDirty);
    }

    [Fact]
    public void HandleKey_Should_ReturnNull_When_EnterOnSaveWithEmptyTitle()
    {
        // arrange
        var task = TaskItemBuilder.Create("a1", "T");
        var form = new TaskEditorForm(task, []);
        form.HandleKey(Key(ConsoleKey.Backspace));
        TabTo(form, 7);

        // act
        var result = form.HandleKey(Key(ConsoleKey.Enter));

        // assert: the form stays open because the required title is empty.
        Assert.Null(result);
    }

    [Fact]
    public void HandleKey_Should_ReturnCancelled_When_Escape()
    {
        // arrange
        var task = TaskItemBuilder.Create("a1", "Title");
        var form = new TaskEditorForm(task, []);

        // act
        var result = form.HandleKey(Key(ConsoleKey.Escape));

        // assert
        Assert.IsType<FormResult.Cancelled>(result);
    }

    [Fact]
    public async Task SubmitAsync_Should_WriteOnlyTitle_When_OnlyTitleChanged()
    {
        // arrange
        var task = TaskItemBuilder.Create(
            "a1", "Title", TaskStates.Open, priority: 2, type: TaskTypes.Task, description: "d", notes: "n");
        var form = new TaskEditorForm(task, ["x"]);
        Type(form, "!");
        var submitted = Save(form);
        var store = new FakeTaskStore { UpdateResult = new TaskUpdateResult { ChangedFields = ["title"] } };

        // act
        var outcome = await form.SubmitAsync(store, submitted.Values, "me", CancellationToken.None);

        // assert
        Assert.Equal("a1", store.UpdatedId);
        var update = store.UpdateReceived!;
        Assert.True(update.TitleGiven);
        Assert.Equal("Title!", update.Title);
        Assert.False(update.StatusGiven);
        Assert.False(update.PriorityGiven);
        Assert.False(update.TypeGiven);
        Assert.False(update.DescriptionGiven);
        Assert.False(update.NotesGiven);
        Assert.Null(store.AddedLabels);
        Assert.Empty(store.RemovedLabels);
        var succeeded = Assert.IsType<TaskEditorOutcome.Succeeded>(outcome);
        Assert.Equal(["title"], succeeded.ChangedFields);
        Assert.Equal("Updated task 'a1'.", succeeded.ToastText);
    }

    [Fact]
    public async Task SubmitAsync_Should_WriteNothing_When_NoFieldChanged()
    {
        // arrange
        var task = TaskItemBuilder.Create("a1", "Title");
        var form = new TaskEditorForm(task, ["x"]);
        var submitted = Save(form);
        var store = new FakeTaskStore();

        // act
        var outcome = await form.SubmitAsync(store, submitted.Values, "me", CancellationToken.None);

        // assert
        Assert.Null(store.UpdatedId);
        Assert.Null(store.AddedLabels);
        Assert.Empty(store.RemovedLabels);
        var succeeded = Assert.IsType<TaskEditorOutcome.Succeeded>(outcome);
        Assert.Empty(succeeded.ChangedFields);
        Assert.Equal("No changes to task 'a1'.", succeeded.ToastText);
    }

    [Fact]
    public async Task SubmitAsync_Should_AddAndRemoveLabels_When_LabelsEdited()
    {
        // arrange: remove "b", add "c", keep "a".
        var task = TaskItemBuilder.Create("a1", "Title");
        var form = new TaskEditorForm(task, ["a", "b"]);
        TabTo(form, 4);
        form.HandleKey(Key(ConsoleKey.DownArrow));
        form.HandleKey(Key('d'));
        form.HandleKey(Key('a'));
        Type(form, "c");
        form.HandleKey(Key(ConsoleKey.Enter));
        var submitted = Save(form);
        var store = new FakeTaskStore();

        // act
        var outcome = await form.SubmitAsync(store, submitted.Values, "me", CancellationToken.None);

        // assert
        Assert.Null(store.UpdatedId);
        Assert.Equal(["c"], store.AddedLabels);
        Assert.Equal(["b"], store.RemovedLabels);
        var succeeded = Assert.IsType<TaskEditorOutcome.Succeeded>(outcome);
        Assert.Equal(["labels"], succeeded.ChangedFields);
    }

    [Fact]
    public async Task SubmitAsync_Should_ReturnFailed_When_StoreThrows()
    {
        // arrange
        var task = TaskItemBuilder.Create("a1", "Title");
        var form = new TaskEditorForm(task, []);
        Type(form, "!");
        var submitted = Save(form);
        var store = new FakeTaskStore { ThrowOnWrite = new ExitException("rejected") };

        // act
        var outcome = await form.SubmitAsync(store, submitted.Values, "me", CancellationToken.None);

        // assert
        var failed = Assert.IsType<TaskEditorOutcome.Failed>(outcome);
        Assert.Equal("rejected", failed.ToastText);
    }

    [Fact]
    public void ToShowToast_Should_UseSuccessStyle_ForSucceeded()
    {
        // arrange
        var outcome = new TaskEditorOutcome.Succeeded([], "No changes to task 'a1'.");

        // act
        var toast = outcome.ToShowToast();

        // assert
        Assert.Equal("No changes to task 'a1'.", toast.Text);
        Assert.Equal(ToastStyle.Success, toast.Style);
    }

    [Fact]
    public void ToShowToast_Should_UseErrorStyle_ForFailed()
    {
        // arrange
        var outcome = new TaskEditorOutcome.Failed("rejected");

        // act
        var toast = outcome.ToShowToast();

        // assert
        Assert.Equal("rejected", toast.Text);
        Assert.Equal(ToastStyle.Error, toast.Style);
    }
}
