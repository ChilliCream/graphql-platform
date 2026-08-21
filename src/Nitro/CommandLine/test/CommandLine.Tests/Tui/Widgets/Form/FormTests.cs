using ChilliCream.Nitro.CommandLine.Tui.Widgets.Form;
using Spectre.Console.Testing;
using FormUnderTest = ChilliCream.Nitro.CommandLine.Tui.Widgets.Form.Form;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Widgets.Form;

public sealed class FormTests
{
    private static ConsoleKeyInfo Key(char c) => new(c, ConsoleKey.NoName, false, false, false);

    private static ConsoleKeyInfo Key(ConsoleKey key, bool shift = false) => new('\0', key, shift, false, false);

    private static ConsoleKeyInfo CtrlKey(ConsoleKey key) => new('\0', key, false, false, true);

    private static FormUnderTest CreateForm(
        Func<FormValue, string?>? titleValidator = null,
        IReadOnlyList<FormButtonSpec>? buttons = null)
    {
        var fields = new FormField[]
        {
            new TextField("title", "Title", validator: titleValidator),
            new SelectField(
                "status",
                "Status",
                [new SelectOption("open", "Open"), new SelectOption("closed", "Closed")])
        };

        var buttonRow = new FormButtons(buttons ?? [
            new FormButtonSpec("save", "Save", ButtonKind.Primary),
            new FormButtonSpec("cancel", "Cancel", ButtonKind.Secondary)
        ]);

        return new FormUnderTest("Edit Task", fields, buttonRow);
    }

    [Fact]
    public void Constructor_Should_Throw_When_NoFields()
    {
        // arrange
        var buttons = new FormButtons([new FormButtonSpec("save", "Save", ButtonKind.Primary)]);

        // act
        var exception = Record.Exception(() => new FormUnderTest("Edit Task", [], buttons));

        // assert
        Assert.IsType<ArgumentException>(exception);
    }

    [Fact]
    public void FocusedField_Should_BeFirstField_Initially()
    {
        // arrange
        var form = CreateForm();

        // assert
        Assert.Equal("title", form.FocusedField?.Id);
    }

    [Fact]
    public void HandleKey_Should_MoveFocusToNextField_When_Tab()
    {
        // arrange
        var form = CreateForm();

        // act
        var result = form.HandleKey(Key(ConsoleKey.Tab));

        // assert
        Assert.Null(result);
        Assert.Equal("status", form.FocusedField?.Id);
    }

    [Fact]
    public void HandleKey_Should_MoveFocusToButtonRow_When_TabPastLastField()
    {
        // arrange
        var form = CreateForm();
        form.HandleKey(Key(ConsoleKey.Tab));

        // act
        form.HandleKey(Key(ConsoleKey.Tab));

        // assert
        Assert.Null(form.FocusedField);
    }

    [Fact]
    public void HandleKey_Should_WrapFocusToFirstField_When_TabPastButtonRow()
    {
        // arrange
        var form = CreateForm();
        form.HandleKey(Key(ConsoleKey.Tab));
        form.HandleKey(Key(ConsoleKey.Tab));

        // act
        form.HandleKey(Key(ConsoleKey.Tab));

        // assert
        Assert.Equal("title", form.FocusedField?.Id);
    }

    [Fact]
    public void HandleKey_Should_MoveFocusBackward_When_ShiftTab()
    {
        // arrange
        var form = CreateForm();
        form.HandleKey(Key(ConsoleKey.Tab));

        // act
        form.HandleKey(Key(ConsoleKey.Tab, shift: true));

        // assert
        Assert.Equal("title", form.FocusedField?.Id);
    }

    [Fact]
    public void HandleKey_Should_TraverseFocus_When_DownArrowOnTextField()
    {
        // arrange: TextField never consumes vertical arrows itself.
        var form = CreateForm();

        // act
        form.HandleKey(Key(ConsoleKey.DownArrow));

        // assert
        Assert.Equal("status", form.FocusedField?.Id);
    }

    [Fact]
    public void HandleKey_Should_TraverseFocus_When_DownArrowOnSelectField()
    {
        // arrange: the select field itself moves with left and right, so down
        // arrow is left for the form to interpret as field-to-field traversal.
        var form = CreateForm();
        form.HandleKey(Key(ConsoleKey.Tab));

        // act
        form.HandleKey(Key(ConsoleKey.DownArrow));

        // assert
        Assert.Null(form.FocusedField);
    }

    [Fact]
    public void HandleKey_Should_NotTraverseFocus_When_RightArrowChangesSelectFieldValue()
    {
        // arrange
        var form = CreateForm();
        form.HandleKey(Key(ConsoleKey.Tab));

        // act: the select field consumes right arrow to change its own selection.
        form.HandleKey(Key(ConsoleKey.RightArrow));

        // assert
        Assert.Equal("status", form.FocusedField?.Id);
    }

    [Fact]
    public void HandleKey_Should_TypeIntoFocusedField_When_CharacterKey()
    {
        // arrange
        var form = CreateForm();

        // act
        form.HandleKey(Key('h'));
        form.HandleKey(Key('i'));

        // assert
        Assert.Equal(new FormValue.Text("hi"), form.FocusedField?.GetValue());
    }

    [Fact]
    public void HandleKey_Should_ReturnCancelled_When_EscapeWhileFieldFocused()
    {
        // arrange
        var form = CreateForm();

        // act
        var result = form.HandleKey(Key(ConsoleKey.Escape));

        // assert
        Assert.IsType<FormResult.Cancelled>(result);
    }

    [Fact]
    public void HandleKey_Should_ReturnCancelled_When_EscapeWhileButtonRowFocused()
    {
        // arrange
        var form = CreateForm();
        form.HandleKey(Key(ConsoleKey.Tab));
        form.HandleKey(Key(ConsoleKey.Tab));

        // act
        var result = form.HandleKey(Key(ConsoleKey.Escape));

        // assert
        Assert.IsType<FormResult.Cancelled>(result);
    }

    [Fact]
    public void HandleKey_Should_ReturnNull_When_EnterWhileFieldFocused()
    {
        // arrange: Enter only activates a button, so it must not submit while
        // focus is still on a field.
        var form = CreateForm();

        // act
        var result = form.HandleKey(Key(ConsoleKey.Enter));

        // assert
        Assert.Null(result);
    }

    [Fact]
    public void HandleKey_Should_Submit_When_EnterOnPrimaryButtonAndValid()
    {
        // arrange
        var form = CreateForm();
        form.HandleKey(Key('h'));
        form.HandleKey(Key(ConsoleKey.Tab));
        form.HandleKey(Key(ConsoleKey.Tab));

        // act
        var result = form.HandleKey(Key(ConsoleKey.Enter));

        // assert
        var submitted = Assert.IsType<FormResult.Submitted>(result);
        Assert.Equal(new FormValue.Text("h"), submitted.Values["title"]);
        Assert.Equal(new FormValue.Text("open"), submitted.Values["status"]);
    }

    [Fact]
    public void HandleKey_Should_BlockSubmit_When_ValidatorFails()
    {
        // arrange
        var form = CreateForm(titleValidator: value =>
            value is FormValue.Text { Value.Length: 0 } ? "Title is required." : null);
        form.HandleKey(Key(ConsoleKey.Tab));
        form.HandleKey(Key(ConsoleKey.Tab));

        // act
        var result = form.HandleKey(Key(ConsoleKey.Enter));

        // assert
        Assert.Null(result);
    }

    [Fact]
    public void HandleKey_Should_Submit_When_CtrlEnterWhileFieldFocused()
    {
        // arrange: focus stays on the first field, no Tab to the button row.
        var form = CreateForm();
        form.HandleKey(Key('h'));

        // act
        var result = form.HandleKey(CtrlKey(ConsoleKey.Enter));

        // assert
        var submitted = Assert.IsType<FormResult.Submitted>(result);
        Assert.Equal(new FormValue.Text("h"), submitted.Values["title"]);
    }

    [Fact]
    public void HandleKey_Should_Submit_When_CtrlSWhileFieldFocused()
    {
        // arrange: Ctrl+S is the fallback chord for terminals that deliver
        // Ctrl+Enter identically to a plain Enter.
        var form = CreateForm();
        form.HandleKey(Key('h'));

        // act
        var result = form.HandleKey(CtrlKey(ConsoleKey.S));

        // assert
        var submitted = Assert.IsType<FormResult.Submitted>(result);
        Assert.Equal(new FormValue.Text("h"), submitted.Values["title"]);
    }

    [Fact]
    public void HandleKey_Should_NotInsertNewline_When_CtrlEnterInTextAreaField()
    {
        // arrange: a plain Enter in a text area inserts a newline, but the
        // save chord must take priority over the field's own key handling.
        var fields = new FormField[] { new TextAreaField("notes", "Notes") };
        var buttons = new FormButtons([new FormButtonSpec("save", "Save", ButtonKind.Primary)]);
        var form = new FormUnderTest("Edit Task", fields, buttons);
        form.HandleKey(Key('h'));

        // act
        var result = form.HandleKey(CtrlKey(ConsoleKey.Enter));

        // assert
        var submitted = Assert.IsType<FormResult.Submitted>(result);
        Assert.Equal(new FormValue.Text("h"), submitted.Values["notes"]);
    }

    [Fact]
    public void HandleKey_Should_NotSubmit_When_PlainEnterInTextAreaField()
    {
        // arrange: confirms the save chord is Ctrl-gated, not a change to
        // the text area's own plain-Enter newline behavior.
        var fields = new FormField[] { new TextAreaField("notes", "Notes") };
        var buttons = new FormButtons([new FormButtonSpec("save", "Save", ButtonKind.Primary)]);
        var form = new FormUnderTest("Edit Task", fields, buttons);

        // act
        var result = form.HandleKey(Key(ConsoleKey.Enter));

        // assert
        Assert.Null(result);
        Assert.Equal(new FormValue.Text("\n"), form.FocusedField?.GetValue());
    }

    [Fact]
    public void HandleKey_Should_FocusFirstInvalidField_When_CtrlEnterAndValidatorFails()
    {
        // arrange: focus sits on the status field, away from the invalid title.
        var form = CreateForm(titleValidator: value =>
            value is FormValue.Text { Value.Length: 0 } ? "Title is required." : null);
        form.HandleKey(Key(ConsoleKey.Tab));

        // act
        var result = form.HandleKey(CtrlKey(ConsoleKey.Enter));

        // assert
        Assert.Null(result);
        Assert.Equal("title", form.FocusedField?.Id);
    }

    [Fact]
    public void HandleKey_Should_IgnoreSelectedButton_When_CtrlEnterOnSecondaryButton()
    {
        // arrange: focus sits on Cancel, but the save chord always targets
        // the primary action, not whichever button is currently selected.
        var form = CreateForm();
        form.HandleKey(Key('h'));
        form.HandleKey(Key(ConsoleKey.Tab));
        form.HandleKey(Key(ConsoleKey.Tab));
        form.HandleKey(Key(ConsoleKey.RightArrow));

        // act
        var result = form.HandleKey(CtrlKey(ConsoleKey.Enter));

        // assert
        var submitted = Assert.IsType<FormResult.Submitted>(result);
        Assert.Equal(new FormValue.Text("h"), submitted.Values["title"]);
    }

    [Fact]
    public void HandleKey_Should_ReturnButtonActivated_When_EnterOnSecondaryButton()
    {
        // arrange
        var form = CreateForm();
        form.HandleKey(Key(ConsoleKey.Tab));
        form.HandleKey(Key(ConsoleKey.Tab));
        form.HandleKey(Key(ConsoleKey.RightArrow));

        // act
        var result = form.HandleKey(Key(ConsoleKey.Enter));

        // assert
        var activated = Assert.IsType<FormResult.ButtonActivated>(result);
        Assert.Equal("cancel", activated.ButtonId);
    }

    [Fact]
    public void HandleKey_Should_NotRequireValidation_When_SecondaryButtonActivated()
    {
        // arrange
        var form = CreateForm(titleValidator: _ => "always invalid");
        form.HandleKey(Key(ConsoleKey.Tab));
        form.HandleKey(Key(ConsoleKey.Tab));
        form.HandleKey(Key(ConsoleKey.RightArrow));

        // act
        var result = form.HandleKey(Key(ConsoleKey.Enter));

        // assert
        Assert.IsType<FormResult.ButtonActivated>(result);
    }

    [Fact]
    public void Render_Should_NotShowValidationError_Initially_When_RequiredFieldEmpty()
    {
        // arrange
        var form = CreateForm(titleValidator: _ => "Title is required.");
        var console = new TestConsole().Width(80).Height(20);

        // act
        console.Write(form.Render(80, 20));

        // assert: the field has neither been touched nor has a submit been
        // attempted, so the error stays hidden.
        Assert.DoesNotContain("Title is required.", console.Output);
    }

    [Fact]
    public void Render_Should_ShowValidationError_When_FieldTouched()
    {
        // arrange
        var form = CreateForm(titleValidator: _ => "always invalid");
        var console = new TestConsole().Width(80).Height(20);

        // act: typing into the title field touches it even though the
        // validator still fails afterwards.
        form.HandleKey(Key('h'));
        console.Write(form.Render(80, 20));

        // assert
        Assert.Contains("always invalid", console.Output);
    }

    [Fact]
    public void Render_Should_ShowValidationError_When_SubmitAttempted_Even_When_FieldUntouched()
    {
        // arrange: never type into the title field, only tab past it.
        var form = CreateForm(titleValidator: _ => "Title is required.");
        var console = new TestConsole().Width(80).Height(20);
        form.HandleKey(Key(ConsoleKey.Tab));
        form.HandleKey(Key(ConsoleKey.Tab));

        // act
        form.HandleKey(Key(ConsoleKey.Enter));
        console.Write(form.Render(80, 20));

        // assert
        Assert.Contains("Title is required.", console.Output);
    }

    [Fact]
    public void Render_Should_IncludeTitleAndFieldLabels()
    {
        // arrange
        var form = CreateForm();
        var console = new TestConsole().Width(80).Height(20);

        // act
        console.Write(form.Render(80, 20));

        // assert
        Assert.Contains("Edit Task", console.Output);
        Assert.Contains("Title", console.Output);
        Assert.Contains("Status", console.Output);
        Assert.Contains("Save", console.Output);
    }

    [Fact]
    public void Render_Should_CapContentWidth_When_FrameIsWiderThanEightyColumns()
    {
        // arrange
        var form = CreateForm();
        var console = new TestConsole().Width(160).Height(20);

        // act
        console.Write(form.Render(160, 20));

        // assert: the outer panel border is centered and never wider than 80
        // columns, however wide the frame around it is.
        var borderLine = console.Output
            .Split('\n')
            .First(line => line.Contains("Edit Task"));
        var leading = borderLine.Length - borderLine.TrimStart(' ').Length;
        var panelWidth = borderLine.Trim().Length;

        Assert.True(panelWidth <= 80);
        Assert.Equal(leading, 160 - leading - panelWidth);
    }

    [Fact]
    public void Render_Should_SeparateFieldsFromButtons_With_BlankLine()
    {
        // arrange
        var form = CreateForm();
        var console = new TestConsole().Width(80).Height(20);

        // act
        console.Write(form.Render(80, 20));

        // assert: the line directly above the button row carries no field
        // content, only the panel's side borders.
        var lines = console.Output.Split('\n');
        var buttonLineIndex = Array.FindIndex(lines, line => line.Contains("Save"));
        var lineAboveButtons = lines[buttonLineIndex - 1];

        Assert.DoesNotContain("Status", lineAboveButtons);
        Assert.DoesNotContain("(o)", lineAboveButtons);
    }

    private static FormUnderTest CreateTallForm(int fieldCount)
    {
        var fields = new List<FormField>();

        for (var i = 0; i < fieldCount; i++)
        {
            fields.Add(new TextAreaField($"field{i}", $"Field {i}"));
        }

        var buttonRow = new FormButtons([
            new FormButtonSpec("save", "Save", ButtonKind.Primary),
            new FormButtonSpec("cancel", "Cancel", ButtonKind.Secondary)
        ]);

        return new FormUnderTest("Edit Task", fields, buttonRow);
    }

    [Fact]
    public void Render_Should_NeverExceedFrameHeight_When_FieldsTallerThanFrame()
    {
        // arrange: 7 text areas (5 rows each = 35) far exceed a 23-row frame.
        var form = CreateTallForm(7);
        var console = new TestConsole().Width(80).Height(30);

        // act
        console.Write(form.Render(80, 23));

        // assert
        var lineCount = console.Output.Split('\n').Length;
        Assert.True(lineCount <= 23, $"expected at most 23 lines, got {lineCount}.");
    }

    [Fact]
    public void Render_Should_ShowFocusedField_When_ScrolledPastTheFirstScreen()
    {
        // arrange: the same oversized form, focus moved to the last field.
        var form = CreateTallForm(7);
        var console = new TestConsole().Width(80).Height(30);

        for (var i = 0; i < 6; i++)
        {
            form.HandleKey(Key(ConsoleKey.Tab));
        }

        // act
        console.Write(form.Render(80, 23));

        // assert: the focused field's label is visible even though it is the
        // seventh of seven fields and the frame cannot show them all at once.
        Assert.Contains("Field 6", console.Output);
    }

    [Fact]
    public void Render_Should_KeepButtonRowVisible_When_FieldsDoNotAllFit()
    {
        // arrange
        var form = CreateTallForm(7);
        var console = new TestConsole().Width(80).Height(30);

        // act: focus stays on the first field, far from the button row.
        console.Write(form.Render(80, 23));

        // assert: the button row is pinned, not scrolled out of view.
        Assert.Contains("Save", console.Output);
    }

    [Fact]
    public void Render_Should_ShowTooSmallNotice_When_FrameBelowHardFloor()
    {
        // arrange
        var form = CreateForm();
        var console = new TestConsole().Width(10).Height(5);

        // act
        console.Write(form.Render(10, 5));

        // assert: the narrow frame wraps the notice onto several lines, so
        // only a fragment that survives wrapping is checked.
        Assert.Contains("too small", console.Output);
    }

    [Fact]
    public void Render_Should_NotShowTooSmallNotice_When_FrameAtMinimumViableSize()
    {
        // arrange: the frame that must remain fully operable per spec.
        var form = CreateForm();
        var console = new TestConsole().Width(80).Height(23);

        // act
        console.Write(form.Render(80, 23));

        // assert
        Assert.DoesNotContain("Terminal too small", console.Output);
    }
}
