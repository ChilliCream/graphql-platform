using ChilliCream.Nitro.CommandLine.Tui.Widgets.Form;
using Spectre.Console;
using Spectre.Console.Rendering;
using Spectre.Console.Testing;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Widgets.Form;

public sealed class SelectFieldTests
{
    private static ConsoleKeyInfo Key(ConsoleKey key) => new('\0', key, false, false, false);

    private static IReadOnlyList<Segment> RenderSegments(IRenderable renderable, TestConsole console, int width)
    {
        var options = RenderOptions.Create(console, console.Profile.Capabilities);

        return [.. renderable.Render(options, width)];
    }

    private static SelectField CreateField(string? initialSelectedId = null) => new(
        "status",
        "Status",
        [new SelectOption("open", "Open"), new SelectOption("closed", "Closed"), new SelectOption("blocked", "Blocked")],
        initialSelectedId: initialSelectedId);

    [Fact]
    public void GetValue_Should_ReturnFirstOption_When_NoInitialSelection()
    {
        // arrange
        var field = CreateField();

        // act
        var value = field.GetValue();

        // assert
        Assert.Equal(new FormValue.Text("open"), value);
    }

    [Fact]
    public void GetValue_Should_ReturnInitialSelection_When_Given()
    {
        // arrange
        var field = CreateField(initialSelectedId: "blocked");

        // act
        var value = field.GetValue();

        // assert
        Assert.Equal(new FormValue.Text("blocked"), value);
    }

    [Fact]
    public void HandleKey_Should_MoveSelectionForward_When_RightArrow()
    {
        // arrange
        var field = CreateField();

        // act
        var handled = field.HandleKey(Key(ConsoleKey.RightArrow));

        // assert
        Assert.True(handled);
        Assert.Equal(new FormValue.Text("closed"), field.GetValue());
    }

    [Fact]
    public void HandleKey_Should_ReturnFalse_When_RightArrowOnLastOption()
    {
        // arrange
        var field = CreateField(initialSelectedId: "blocked");

        // act
        var handled = field.HandleKey(Key(ConsoleKey.RightArrow));

        // assert
        Assert.False(handled);
        Assert.Equal(new FormValue.Text("blocked"), field.GetValue());
    }

    [Fact]
    public void HandleKey_Should_ReturnFalse_When_LeftArrowOnFirstOption()
    {
        // arrange
        var field = CreateField();

        // act
        var handled = field.HandleKey(Key(ConsoleKey.LeftArrow));

        // assert
        Assert.False(handled);
    }

    [Theory]
    [InlineData(ConsoleKey.UpArrow)]
    [InlineData(ConsoleKey.DownArrow)]
    public void HandleKey_Should_ReturnFalse_When_VerticalArrow(ConsoleKey key)
    {
        // arrange
        var field = CreateField();

        // act
        var handled = field.HandleKey(Key(key));

        // assert
        Assert.False(handled);
        Assert.Equal(new FormValue.Text("open"), field.GetValue());
    }

    [Fact]
    public void Render_Should_MarkSelectedOption()
    {
        // arrange
        var field = CreateField(initialSelectedId: "closed");
        var console = new TestConsole().Width(60);

        // act
        console.Write(field.Render(40, focused: false));

        // assert
        Assert.Contains("(o) Closed", console.Output);
        Assert.Contains("( ) Open", console.Output);
    }

    [Fact]
    public void Render_Should_KeepSelectedOptionVisible_When_FocusedFieldHasHeightBudget()
    {
        // arrange
        var field = new SelectField(
            "type",
            "Type",
            [
                new SelectOption("task", "Task"),
                new SelectOption("bug", "Bug"),
                new SelectOption("feature", "Feature"),
                new SelectOption("epic", "Epic"),
                new SelectOption("chore", "Chore"),
                new SelectOption("docs", "Docs"),
                new SelectOption("question", "Question")
            ],
            initialSelectedId: "question");
        var console = new TestConsole().Width(20);
        var rendered = field.Render(20, focused: true, maxHeight: 3);

        // act
        console.Write(rendered);
        var segments = RenderSegments(rendered, console, 20);

        // assert
        Assert.True(Segment.SplitLines(segments).Count <= 3);
        Assert.Contains("(o) Question", console.Output);
    }

    [Fact]
    public void Render_Should_RenderOptionsOnOneLine_When_TheyFitTheWidth()
    {
        // arrange
        var field = CreateField();
        var console = new TestConsole().Width(60);

        // act
        console.Write(field.Render(40, focused: false));

        // assert: all three options share one interior row.
        var interiorLines = console.Output
            .Split('\n')
            .Where(line => line.Contains("(o)") || line.Contains("( )"))
            .ToArray();
        Assert.Single(interiorLines);
        Assert.Contains("Open", interiorLines[0]);
        Assert.Contains("Closed", interiorLines[0]);
        Assert.Contains("Blocked", interiorLines[0]);
    }

    [Fact]
    public void Render_Should_WrapOptionsToNextLine_When_TheyDoNotFitTheWidth()
    {
        // arrange: a field width far too narrow for three options on one line.
        var field = CreateField();
        var console = new TestConsole().Width(30);

        // act
        console.Write(field.Render(14, focused: false));

        // assert
        var interiorLines = console.Output
            .Split('\n')
            .Where(line => line.Contains("(o)") || line.Contains("( )"))
            .ToArray();
        Assert.True(interiorLines.Length > 1);
    }
}
