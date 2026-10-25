using ChilliCream.Nitro.CommandLine.Tui.Editing;
using Spectre.Console.Testing;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Editing;

public sealed class QuickPickerTests
{
    private static ConsoleKeyInfo Key(ConsoleKey key) => new('\0', key, false, false, false);

    private static QuickPicker CreatePicker(string? initialSelectedId = null) => new(
        "Status",
        [
            new QuickPickerOption("open", "Open"),
            new QuickPickerOption("in_progress", "In Progress"),
            new QuickPickerOption("blocked", "Blocked")
        ],
        initialSelectedId);

    [Fact]
    public void HandleKey_Should_ApplyFirstOption_When_EnterPressedWithNoMovement()
    {
        // arrange
        var picker = CreatePicker();

        // act
        var result = picker.HandleKey(Key(ConsoleKey.Enter));

        // assert
        var applied = Assert.IsType<QuickPickerResult.Applied>(result);
        Assert.Equal("open", applied.SelectedId);
    }

    [Fact]
    public void HandleKey_Should_ApplyPreSelectedOption_When_InitialSelectedIdGiven()
    {
        // arrange
        var picker = CreatePicker(initialSelectedId: "blocked");

        // act
        var result = picker.HandleKey(Key(ConsoleKey.Enter));

        // assert
        var applied = Assert.IsType<QuickPickerResult.Applied>(result);
        Assert.Equal("blocked", applied.SelectedId);
    }

    [Fact]
    public void HandleKey_Should_MoveSelectionDown_When_JOrDownArrowPressed()
    {
        // arrange
        var picker = CreatePicker();
        picker.HandleKey(Key(ConsoleKey.J));

        // act
        var result = picker.HandleKey(Key(ConsoleKey.Enter));

        // assert
        var applied = Assert.IsType<QuickPickerResult.Applied>(result);
        Assert.Equal("in_progress", applied.SelectedId);
    }

    [Fact]
    public void HandleKey_Should_StopAtLastOption_When_MovingDownPastTheEnd()
    {
        // arrange
        var picker = CreatePicker();
        picker.HandleKey(Key(ConsoleKey.DownArrow));
        picker.HandleKey(Key(ConsoleKey.DownArrow));
        picker.HandleKey(Key(ConsoleKey.DownArrow));

        // act
        var result = picker.HandleKey(Key(ConsoleKey.Enter));

        // assert
        var applied = Assert.IsType<QuickPickerResult.Applied>(result);
        Assert.Equal("blocked", applied.SelectedId);
    }

    [Fact]
    public void HandleKey_Should_MoveSelectionUp_When_KOrUpArrowPressed()
    {
        // arrange
        var picker = CreatePicker(initialSelectedId: "blocked");
        picker.HandleKey(Key(ConsoleKey.K));

        // act
        var result = picker.HandleKey(Key(ConsoleKey.Enter));

        // assert
        var applied = Assert.IsType<QuickPickerResult.Applied>(result);
        Assert.Equal("in_progress", applied.SelectedId);
    }

    [Fact]
    public void HandleKey_Should_StopAtFirstOption_When_MovingUpPastTheStart()
    {
        // arrange
        var picker = CreatePicker();

        // act
        picker.HandleKey(Key(ConsoleKey.UpArrow));
        var result = picker.HandleKey(Key(ConsoleKey.Enter));

        // assert
        var applied = Assert.IsType<QuickPickerResult.Applied>(result);
        Assert.Equal("open", applied.SelectedId);
    }

    [Fact]
    public void HandleKey_Should_ReturnCancelled_When_EscapePressed()
    {
        // arrange
        var picker = CreatePicker();

        // act
        var result = picker.HandleKey(Key(ConsoleKey.Escape));

        // assert
        Assert.IsType<QuickPickerResult.Cancelled>(result);
    }

    [Fact]
    public void HandleKey_Should_ReturnNull_When_MovementKeyPressed()
    {
        // arrange
        var picker = CreatePicker();

        // act
        var result = picker.HandleKey(Key(ConsoleKey.J));

        // assert
        Assert.Null(result);
    }

    [Fact]
    public void Render_Should_IncludeTitleAndOptionLabels()
    {
        // arrange
        var picker = CreatePicker();
        var console = new TestConsole().Width(60).Height(10);

        // act
        console.Write(picker.Render(60, 10));

        // assert
        Assert.Contains("Status", console.Output);
        Assert.Contains("Open", console.Output);
        Assert.Contains("In Progress", console.Output);
        Assert.Contains("Blocked", console.Output);
    }

    [Fact]
    public void Constructor_Should_Throw_When_TitleEmpty()
    {
        // act
        var exception = Record.Exception(
            () => new QuickPicker("", [new QuickPickerOption("a", "A")]));

        // assert
        Assert.IsType<ArgumentException>(exception);
    }

    [Fact]
    public void Constructor_Should_Throw_When_OptionsEmpty()
    {
        // act
        var exception = Record.Exception(() => new QuickPicker("Status", []));

        // assert
        Assert.IsType<ArgumentException>(exception);
    }
}
