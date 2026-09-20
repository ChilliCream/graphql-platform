using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Tui.Editing;
using ChilliCream.Nitro.CommandLine.Tui.Input;
using ChilliCream.Nitro.CommandLine.Tui.Widgets.Form;
using Spectre.Console;
using Spectre.Console.Rendering;
using Spectre.Console.Testing;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Editing;

public sealed class TaskEditorFormTests
{
    private static ConsoleKeyInfo Key(char c) => new(c, ConsoleKey.NoName, false, false, false);

    private static ConsoleKeyInfo Key(ConsoleKey key) => new('\0', key, false, false, false);

    private static ConsoleKeyInfo CtrlKey(ConsoleKey key) => new('\0', key, false, false, true);

    private static IReadOnlyList<Segment> RenderSegments(IRenderable renderable, TestConsole console, int width)
    {
        var options = RenderOptions.Create(console, console.Profile.Capabilities);

        return [.. renderable.Render(options, width)];
    }

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
        // arrange
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

        // assert
        Assert.Null(result);
    }

    [Fact]
    public void HandleKey_Should_ReturnNull_When_EnterOnSaveWithWhitespaceOnlyTitle()
    {
        // arrange
        var task = TaskItemBuilder.Create("a1", "T");
        var form = new TaskEditorForm(task, []);
        form.HandleKey(Key(ConsoleKey.Backspace));
        Type(form, "   ");
        TabTo(form, 7);

        // act
        var result = form.HandleKey(Key(ConsoleKey.Enter));

        // assert
        Assert.Null(result);
    }

    [Fact]
    public void HandleKey_Should_Submit_When_CtrlEnterFromTitleField()
    {
        // arrange
        var task = TaskItemBuilder.Create("a1", "Title");
        var form = new TaskEditorForm(task, []);
        Type(form, "!");

        // act
        var result = form.HandleKey(CtrlKey(ConsoleKey.Enter));

        // assert
        var submitted = Assert.IsType<FormResult.Submitted>(result);
        Assert.Equal(new FormValue.Text("Title!"), submitted.Values[TaskEditorForm.TitleFieldId]);
    }

    [Fact]
    public void HandleKey_Should_Submit_When_CtrlSFromTitleField()
    {
        // arrange
        var task = TaskItemBuilder.Create("a1", "Title");
        var form = new TaskEditorForm(task, []);
        Type(form, "!");

        // act
        var result = form.HandleKey(CtrlKey(ConsoleKey.S));

        // assert
        var submitted = Assert.IsType<FormResult.Submitted>(result);
        Assert.Equal(new FormValue.Text("Title!"), submitted.Values[TaskEditorForm.TitleFieldId]);
    }

    [Fact]
    public void HandleKey_Should_NotClose_When_CtrlEnterWithEmptyTitle()
    {
        // arrange
        var task = TaskItemBuilder.Create("a1", "T");
        var form = new TaskEditorForm(task, []);
        form.HandleKey(Key(ConsoleKey.Backspace));

        // act
        var result = form.HandleKey(CtrlKey(ConsoleKey.Enter));

        // assert
        Assert.Null(result);
        Assert.Equal(TaskEditorForm.TitleFieldId, form.FocusedField?.Id);
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
    public async Task SubmitAsync_Should_ReportUpdated_When_OnlyStatusChanged()
    {
        // arrange
        var task = TaskItemBuilder.Create("a1", "Title", TaskStates.Open, priority: TaskPriorities.Medium);
        var form = new TaskEditorForm(task, []);
        TabTo(form, 1);
        form.HandleKey(Key(ConsoleKey.RightArrow));
        var submitted = Save(form);
        var store = new FakeTaskStore();

        // act
        var outcome = await form.SubmitAsync(store, submitted.Values, "me", CancellationToken.None);

        // assert
        Assert.True(store.UpdateReceived!.StatusGiven);
        Assert.False(store.UpdateReceived.PriorityGiven);
        var succeeded = Assert.IsType<TaskEditorOutcome.Succeeded>(outcome);
        Assert.Equal(["status"], succeeded.ChangedFields);
        Assert.Equal("Updated task 'a1'.", succeeded.ToastText);
    }

    [Fact]
    public async Task SubmitAsync_Should_ReportUpdated_When_OnlyPriorityChanged()
    {
        // arrange
        var task = TaskItemBuilder.Create("a1", "Title", TaskStates.Open, priority: TaskPriorities.Medium);
        var form = new TaskEditorForm(task, []);
        TabTo(form, 2);
        form.HandleKey(Key(ConsoleKey.RightArrow));
        var submitted = Save(form);
        var store = new FakeTaskStore();

        // act
        var outcome = await form.SubmitAsync(store, submitted.Values, "me", CancellationToken.None);

        // assert
        Assert.False(store.UpdateReceived!.StatusGiven);
        Assert.True(store.UpdateReceived.PriorityGiven);
        var succeeded = Assert.IsType<TaskEditorOutcome.Succeeded>(outcome);
        Assert.Equal(["priority"], succeeded.ChangedFields);
        Assert.Equal("Updated task 'a1'.", succeeded.ToastText);
    }

    [Fact]
    public async Task SubmitAsync_Should_AddAndRemoveLabels_When_LabelsEdited()
    {
        // arrange
        // Remove "b", add "c", and keep "a".
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

    [Fact]
    public void Render_Should_KeepSelectedQuestionAndSaveVisible_When_TypeFieldExceedsFrameBudget()
    {
        // arrange
        var task = TaskItemBuilder.Create("a1", "Title", type: TaskTypes.Question);
        var form = new TaskEditorForm(task, []);
        var console = new TestConsole().Width(24).Height(10);
        TabTo(form, 3);

        // act
        var segments = RenderSegments(form.Render(24, 10), console, 24);

        // assert
        Assert.True(Segment.SplitLines(segments).Count <= 10);
        Assert.Contains(segments, segment => segment.Text.Contains("Question", StringComparison.Ordinal));
        Assert.Contains(segments, segment => segment.Text.Contains("Save", StringComparison.Ordinal));
    }

    [Fact]
    public void HandleKey_Should_ReachEveryFieldAndSave_When_FrameIsMinimumViableSize()
    {
        // arrange
        // the 80x24 frame (23 content rows once the status row is reserved) must stay fully operable
        var task = TaskItemBuilder.Create("a1", "Title");
        var form = new TaskEditorForm(task, ["alpha"]);
        var fieldLabels = new[] { "Title", "Status", "Priority", "Type", "Labels", "Description", "Notes" };
        var focusedFieldFrames = new List<string>();

        // act
        // every field is reachable by Tab and, once focused, is fully rendered in a fresh frame
        foreach (var label in fieldLabels)
        {
            var console = new TestConsole().Width(80).Height(23);
            console.Write(form.Render(80, 23));
            var lines = console.Output.Split('\n');
            var fieldStart = Array.FindIndex(lines, line => line.Contains($"╭─{label}"));
            var fieldEnd = Array.FindIndex(lines, fieldStart, line => line.Contains("╰"));
            focusedFieldFrames.Add(string.Join("\n", lines[fieldStart..(fieldEnd + 1)]));
            form.HandleKey(Key(ConsoleKey.Tab));
        }

        // the button row is the next and final stop
        var buttonConsole = new TestConsole().Width(80).Height(23);
        buttonConsole.Write(form.Render(80, 23));
        var buttonLines = buttonConsole.Output.Split('\n');
        var buttonLineIndex = Array.FindIndex(buttonLines, line => line.Contains("Save"));
        var buttonFrame = string.Join("\n", buttonLines[(buttonLineIndex - 1)..(buttonLineIndex + 2)]);

        // assert
        focusedFieldFrames.MatchInlineSnapshots(
            [
                """
                │ ╭─Title *──────────────────────────────────────────────────────────────────╮ │
                │ │ Title                                                                    │ │
                │ ╰──────────────────────────────────────────────────────────────────────────╯ │
                """,
                """
                │ ╭─Status───────────────────────────────────────────────────────────────────╮ │
                │ │ (o) Open  ( ) In Progress  ( ) Blocked  ( ) Deferred                     │ │
                │ ╰──────────────────────────────────────────────────────────────────────────╯ │
                """,
                """
                │ ╭─Priority─────────────────────────────────────────────────────────────────╮ │
                │ │ ( ) P0  ( ) P1  (o) P2  ( ) P3  ( ) P4                                   │ │
                │ ╰──────────────────────────────────────────────────────────────────────────╯ │
                """,
                """
                │ ╭─Type─────────────────────────────────────────────────────────────────────╮ │
                │ │ (o) Task  ( ) Bug  ( ) Feature  ( ) Epic  ( ) Chore  ( ) Docs            │ │
                │ │ ( ) Question                                                             │ │
                │ ╰──────────────────────────────────────────────────────────────────────────╯ │
                """,
                """
                │ ╭─Labels───────────────────────────────────────────────────────────────────╮ │
                │ │ - alpha                                                                  │ │
                │ ╰──────────────────────────────────────────────────────────────────────────╯ │
                """,
                """
                │ ╭─Description──────────────────────────────────────────────────────────────╮ │
                │ │                                                                          │ │
                │ │                                                                          │ │
                │ │                                                                          │ │
                │ ╰──────────────────────────────────────────────────────────────────────────╯ │
                """,
                """
                │ ╭─Notes────────────────────────────────────────────────────────────────────╮ │
                │ │                                                                          │ │
                │ │                                                                          │ │
                │ │                                                                          │ │
                │ ╰──────────────────────────────────────────────────────────────────────────╯ │
                """
            ]);
        buttonFrame.MatchInlineSnapshot(
            """
            │                                                                              │
            │  Save                           Cancel                                       │
            ╰──────────────────────────────────────────────────────────────────────────────╯
            """);

        // act
        var result = form.HandleKey(Key(ConsoleKey.Enter));

        // assert
        // save works from the fully-scrolled button row
        Assert.IsType<FormResult.Submitted>(result);
    }
}
