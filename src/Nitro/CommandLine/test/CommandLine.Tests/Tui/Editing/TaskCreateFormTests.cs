using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Tui.Editing;
using ChilliCream.Nitro.CommandLine.Tui.Input;
using ChilliCream.Nitro.CommandLine.Tui.Widgets.Form;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Editing;

public sealed class TaskCreateFormTests
{
    private static ConsoleKeyInfo Key(char c) => new(c, ConsoleKey.NoName, false, false, false);

    private static ConsoleKeyInfo Key(ConsoleKey key) => new('\0', key, false, false, false);

    private static void Type(TaskCreateForm form, string text)
    {
        foreach (var c in text)
        {
            form.HandleKey(Key(c));
        }
    }

    private static void TabTo(TaskCreateForm form, int times)
    {
        for (var i = 0; i < times; i++)
        {
            form.HandleKey(Key(ConsoleKey.Tab));
        }
    }

    private static FormResult.Submitted Save(TaskCreateForm form)
    {
        // Tab from wherever focus currently sits until it reaches the button
        // row (FocusedField null), then activate the default Create button.
        while (form.FocusedField is not null)
        {
            form.HandleKey(Key(ConsoleKey.Tab));
        }

        return Assert.IsType<FormResult.Submitted>(form.HandleKey(Key(ConsoleKey.Enter)));
    }

    [Fact]
    public void Constructor_Should_PresetType_ToGivenTypePreset()
    {
        // arrange
        var form = new TaskCreateForm(TaskTypes.Epic);

        // act
        TabTo(form, 1);

        // assert
        Assert.Equal(new FormValue.Text(TaskTypes.Epic), form.FocusedField?.GetValue());
    }

    [Fact]
    public void Constructor_Should_DefaultPriority_ToMedium()
    {
        // arrange
        var form = new TaskCreateForm(TaskTypes.Task);

        // act
        TabTo(form, 2);

        // assert
        Assert.Equal(new FormValue.Text("2"), form.FocusedField?.GetValue());
    }

    [Fact]
    public void Constructor_Should_LeaveLabelsAndDescription_Empty()
    {
        // arrange
        var form = new TaskCreateForm(TaskTypes.Task);

        // act
        TabTo(form, 3);

        // assert
        Assert.Equal(new FormValue.List([]), form.FocusedField?.GetValue());

        TabTo(form, 1);
        Assert.Equal(new FormValue.Text(""), form.FocusedField?.GetValue());
    }

    [Fact]
    public void IsDirty_Should_BeFalse_Initially()
    {
        // arrange
        var form = new TaskCreateForm(TaskTypes.Task);

        // assert
        Assert.False(form.IsDirty);
    }

    [Fact]
    public void IsDirty_Should_BeTrue_When_TitleEdited()
    {
        // arrange
        var form = new TaskCreateForm(TaskTypes.Task);

        // act
        form.HandleKey(Key('!'));

        // assert
        Assert.True(form.IsDirty);
    }

    [Fact]
    public void HandleKey_Should_ReturnNull_When_EnterOnCreateWithEmptyTitle()
    {
        // arrange
        var form = new TaskCreateForm(TaskTypes.Task);
        TabTo(form, 5);

        // act
        var result = form.HandleKey(Key(ConsoleKey.Enter));

        // assert: the form stays open because the required title is empty.
        Assert.Null(result);
    }

    [Fact]
    public void HandleKey_Should_ReturnNull_When_EnterOnCreateWithWhitespaceOnlyTitle()
    {
        // arrange
        var form = new TaskCreateForm(TaskTypes.Task);
        Type(form, "   ");
        TabTo(form, 5);

        // act
        var result = form.HandleKey(Key(ConsoleKey.Enter));

        // assert
        Assert.Null(result);
    }

    [Fact]
    public void HandleKey_Should_ReturnCancelled_When_Escape()
    {
        // arrange
        var form = new TaskCreateForm(TaskTypes.Task);

        // act
        var result = form.HandleKey(Key(ConsoleKey.Escape));

        // assert
        Assert.IsType<FormResult.Cancelled>(result);
    }

    [Fact]
    public async Task SubmitAsync_Should_CreateTopLevelTask_When_NoParentId()
    {
        // arrange
        var form = new TaskCreateForm(TaskTypes.Task);
        Type(form, "Fix the parser");
        var submitted = Save(form);
        var store = new FakeTaskStore { CreationResult = new TaskCreationResult { Id = "a1" } };

        // act
        var outcome = await form.SubmitAsync(store, submitted.Values, "me", CancellationToken.None);

        // assert
        var creation = store.CreationReceived!;
        Assert.Equal("Fix the parser", creation.Title);
        Assert.Equal(TaskTypes.Task, creation.Type);
        Assert.Equal(TaskPriorities.Medium, creation.Priority);
        Assert.Null(creation.ParentId);
        Assert.Equal("me", creation.Actor);
        var succeeded = Assert.IsType<TaskCreateOutcome.Succeeded>(outcome);
        Assert.Equal("a1", succeeded.TaskId);
        Assert.Equal("Created task 'a1'.", succeeded.ToastText);
    }

    [Fact]
    public async Task SubmitAsync_Should_PassParentId_Through_ToTheStore()
    {
        // arrange: a task created as the child of the currently selected task.
        var form = new TaskCreateForm(TaskTypes.Task, parentId: "app-1a2");
        Type(form, "Sub task");
        var submitted = Save(form);
        var store = new FakeTaskStore { CreationResult = new TaskCreationResult { Id = "app-1a2.3" } };

        // act
        var outcome = await form.SubmitAsync(store, submitted.Values, "me", CancellationToken.None);

        // assert
        Assert.Equal("app-1a2", store.CreationReceived!.ParentId);
        var succeeded = Assert.IsType<TaskCreateOutcome.Succeeded>(outcome);
        Assert.Equal("app-1a2.3", succeeded.TaskId);
    }

    [Fact]
    public void Constructor_Should_DefaultParentField_ToChild_When_ParentIdGiven()
    {
        // arrange
        var form = new TaskCreateForm(TaskTypes.Task, parentId: "app-1a2");

        // act
        TabTo(form, 2);

        // assert
        Assert.Equal(new FormValue.Text(TaskCreateForm.ChildParentOptionId), form.FocusedField?.GetValue());
    }

    [Fact]
    public async Task SubmitAsync_Should_CreateTopLevelTask_When_ParentFieldSwitchedToNoParent()
    {
        // arrange: the board always has a selection once a column is
        // populated, so this is the only gesture that lets a root task be
        // created while a row is selected.
        var form = new TaskCreateForm(TaskTypes.Task, parentId: "app-1a2");
        Type(form, "Root task");
        TabTo(form, 2);
        form.HandleKey(Key(ConsoleKey.RightArrow));
        var submitted = Save(form);
        var store = new FakeTaskStore { CreationResult = new TaskCreationResult { Id = "a2" } };

        // act
        var outcome = await form.SubmitAsync(store, submitted.Values, "me", CancellationToken.None);

        // assert
        Assert.Null(store.CreationReceived!.ParentId);
        var succeeded = Assert.IsType<TaskCreateOutcome.Succeeded>(outcome);
        Assert.Equal("a2", succeeded.TaskId);
    }

    [Fact]
    public void IsDirty_Should_BeTrue_When_ParentFieldSwitchedToNoParent()
    {
        // arrange
        var form = new TaskCreateForm(TaskTypes.Task, parentId: "app-1a2");
        TabTo(form, 2);

        // act
        form.HandleKey(Key(ConsoleKey.RightArrow));

        // assert
        Assert.True(form.IsDirty);
    }

    [Fact]
    public async Task SubmitAsync_Should_CreateEpic_When_BuiltWithEpicPreset()
    {
        // arrange
        var form = new TaskCreateForm(TaskTypes.Epic);
        Type(form, "Search overhaul");
        var submitted = Save(form);
        var store = new FakeTaskStore();

        // act
        await form.SubmitAsync(store, submitted.Values, "me", CancellationToken.None);

        // assert
        Assert.Equal(TaskTypes.Epic, store.CreationReceived!.Type);
    }

    [Fact]
    public async Task SubmitAsync_Should_ReturnFailed_When_StoreThrows()
    {
        // arrange
        var form = new TaskCreateForm(TaskTypes.Task);
        Type(form, "Title");
        var submitted = Save(form);
        var store = new FakeTaskStore { ThrowOnWrite = new ExitException("rejected") };

        // act
        var outcome = await form.SubmitAsync(store, submitted.Values, "me", CancellationToken.None);

        // assert
        var failed = Assert.IsType<TaskCreateOutcome.Failed>(outcome);
        Assert.Equal("rejected", failed.ToastText);
    }

    [Fact]
    public void ToShowToast_Should_UseSuccessStyle_ForSucceeded()
    {
        // arrange
        var outcome = new TaskCreateOutcome.Succeeded("a1", "Created task 'a1'.");

        // act
        var toast = outcome.ToShowToast();

        // assert
        Assert.Equal("Created task 'a1'.", toast.Text);
        Assert.Equal(ToastStyle.Success, toast.Style);
    }

    [Fact]
    public void ToShowToast_Should_UseErrorStyle_ForFailed()
    {
        // arrange
        var outcome = new TaskCreateOutcome.Failed("rejected");

        // act
        var toast = outcome.ToShowToast();

        // assert
        Assert.Equal("rejected", toast.Text);
        Assert.Equal(ToastStyle.Error, toast.Style);
    }
}
