using ChilliCream.Nitro.CommandLine.Tui.Editing;
using ChilliCream.Nitro.CommandLine.Tui.Widgets.Form;
using Spectre.Console.Testing;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Editing;

public sealed class ConfirmDialogTests
{
    private static ConsoleKeyInfo Key(ConsoleKey key) => new('\0', key, false, false, false);

    private static ConsoleKeyInfo Char(char c) => new(c, ConsoleKey.NoName, false, false, false);

    private static ConfirmDialog CreateDialog() => new("Close task 'a1': \"Title\"?", "Close");

    [Fact]
    public void HandleKey_Should_ReturnCancelled_When_EscapePressedAtField()
    {
        // arrange
        var dialog = CreateDialog();

        // act
        var result = dialog.HandleKey(Key(ConsoleKey.Escape));

        // assert
        Assert.IsType<ConfirmDialogResult.Cancelled>(result);
    }

    [Fact]
    public void HandleKey_Should_ReturnConfirmedWithEmptyReason_When_EnterPressedAtFieldWithNoText()
    {
        // arrange
        var dialog = CreateDialog();

        // act
        var result = dialog.HandleKey(Key(ConsoleKey.Enter));

        // assert
        var confirmed = Assert.IsType<ConfirmDialogResult.Confirmed>(result);
        Assert.Equal("", confirmed.Reason);
    }

    [Fact]
    public void HandleKey_Should_ReturnConfirmedWithTypedReason_When_TextEnteredThenEnter()
    {
        // arrange
        var dialog = CreateDialog();
        dialog.HandleKey(Char('n'));
        dialog.HandleKey(Char('o'));

        // act
        var result = dialog.HandleKey(Key(ConsoleKey.Enter));

        // assert
        var confirmed = Assert.IsType<ConfirmDialogResult.Confirmed>(result);
        Assert.Equal("no", confirmed.Reason);
    }

    [Fact]
    public void HandleKey_Should_ReturnNull_When_TabMovesFocusToButtons()
    {
        // arrange
        var dialog = CreateDialog();

        // act
        var result = dialog.HandleKey(Key(ConsoleKey.Tab));

        // assert
        Assert.Null(result);
    }

    [Fact]
    public void HandleKey_Should_ReturnConfirmed_When_EnterActivatesDefaultButtonAfterTab()
    {
        // arrange: the confirm button is selected by default among the buttons.
        var dialog = CreateDialog();
        dialog.HandleKey(Key(ConsoleKey.Tab));

        // act
        var result = dialog.HandleKey(Key(ConsoleKey.Enter));

        // assert
        var confirmed = Assert.IsType<ConfirmDialogResult.Confirmed>(result);
        Assert.Equal("", confirmed.Reason);
    }

    [Fact]
    public void HandleKey_Should_ReturnCancelled_When_CancelButtonActivated()
    {
        // arrange
        var dialog = CreateDialog();
        dialog.HandleKey(Key(ConsoleKey.Tab));
        dialog.HandleKey(Key(ConsoleKey.RightArrow));

        // act
        var result = dialog.HandleKey(Key(ConsoleKey.Enter));

        // assert
        Assert.IsType<ConfirmDialogResult.Cancelled>(result);
    }

    [Fact]
    public void HandleKey_Should_ReturnCancelled_When_EscapePressedAtButtons()
    {
        // arrange
        var dialog = CreateDialog();
        dialog.HandleKey(Key(ConsoleKey.Tab));

        // act
        var result = dialog.HandleKey(Key(ConsoleKey.Escape));

        // assert
        Assert.IsType<ConfirmDialogResult.Cancelled>(result);
    }

    [Fact]
    public void Render_Should_IncludeMessage()
    {
        // arrange
        var dialog = CreateDialog();
        var console = new TestConsole().Width(60).Height(10);

        // act
        console.Write(dialog.Render(60, 10));

        // assert
        Assert.Contains("Close task", console.Output);
    }

    [Fact]
    public void Constructor_Should_Throw_When_MessageEmpty()
    {
        // act
        var exception = Record.Exception(() => new ConfirmDialog("", "Close"));

        // assert
        Assert.IsType<ArgumentException>(exception);
    }
}
