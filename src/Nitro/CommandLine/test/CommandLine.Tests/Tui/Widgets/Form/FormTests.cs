using ChilliCream.Nitro.CommandLine.Tui.Widgets.Form;
using Spectre.Console.Testing;
using FormUnderTest = ChilliCream.Nitro.CommandLine.Tui.Widgets.Form.Form;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Widgets.Form;

public sealed class FormTests
{
    private static ConsoleKeyInfo Key(char c) => new(c, ConsoleKey.NoName, false, false, false);

    private static ConsoleKeyInfo Key(ConsoleKey key, bool shift = false) => new('\0', key, shift, false, false);

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
    public void HandleKey_Should_NotTraverseFocus_When_DownArrowChangesSelectFieldValue()
    {
        // arrange
        var form = CreateForm();
        form.HandleKey(Key(ConsoleKey.Tab));

        // act: the select field consumes down arrow to change its own selection.
        form.HandleKey(Key(ConsoleKey.DownArrow));

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
}
