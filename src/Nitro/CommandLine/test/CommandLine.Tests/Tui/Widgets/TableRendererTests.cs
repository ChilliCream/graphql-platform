using ChilliCream.Nitro.CommandLine.Tui.Widgets;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Widgets;

public sealed class TableRendererTests
{
    // ROLE drops first (priority 0), then AGE (priority 1); NAME has no drop priority so it is
    // never dropped. With the four-space gutter, all three need 19 columns, NAME+AGE need 11,
    // and NAME alone needs 4.
    private static readonly IReadOnlyList<TableColumnSpec> s_columns =
    [
        new TableColumnSpec("NAME", MinWidth: 4),
        new TableColumnSpec("ROLE", MinWidth: 4, DropPriority: 0),
        new TableColumnSpec("AGE", MinWidth: 3, DropPriority: 1, Alignment: ColumnAlignment.Right)
    ];

    [Fact]
    public void ComputeWidths_Should_UseMinimumWidth_When_TitlesAndValuesAreShort()
    {
        // arrange
        IReadOnlyList<IReadOnlyList<string>> rows = [["a", "b", "1"]];

        // act
        var widths = TableLayout.ComputeWidths(s_columns, rows);

        // assert
        Assert.Equal([4, 4, 3], widths);
    }

    [Fact]
    public void ComputeWidths_Should_GrowToLongestValue_When_ARowIsWiderThanMinimum()
    {
        // arrange
        IReadOnlyList<IReadOnlyList<string>> rows = [["a-very-long-name", "b", "1"]];

        // act
        var widths = TableLayout.ComputeWidths(s_columns, rows);

        // assert
        Assert.Equal(16, widths[0]);
    }

    [Fact]
    public void ComputeWidths_Should_IgnoreRowValues_When_ColumnHasAFixedWidth()
    {
        // arrange
        IReadOnlyList<TableColumnSpec> columns = [new TableColumnSpec("X", MinWidth: 1, FixedWidth: 5)];
        IReadOnlyList<IReadOnlyList<string>> rows = [["a-value-much-wider-than-five"]];

        // act
        var widths = TableLayout.ComputeWidths(columns, rows);

        // assert
        Assert.Equal(5, widths[0]);
    }

    [Fact]
    public void Plan_Should_ShowEveryColumn_When_BudgetFitsThemAll()
    {
        // arrange
        int[] widths = [4, 4, 3];

        // act
        var layout = TableLayout.Plan(budget: 19, s_columns, widths);

        // assert
        Assert.Equal([true, true, true], layout.Select(c => c.Visible));
    }

    [Fact]
    public void Plan_Should_DropLowestPriorityColumnFirst_When_BudgetIsTooNarrow()
    {
        // arrange
        // NAME(4) + gutter(4) + AGE(3) fits in 11, but not with ROLE(4) + gutter too.
        int[] widths = [4, 4, 3];

        // act
        var layout = TableLayout.Plan(budget: 15, s_columns, widths);

        // assert
        Assert.Equal([true, false, true], layout.Select(c => c.Visible));
    }

    [Fact]
    public void Plan_Should_KeepDroppingByPriority_When_BudgetStaysTooNarrow()
    {
        // arrange
        int[] widths = [4, 4, 3];

        // act
        var layout = TableLayout.Plan(budget: 8, s_columns, widths);

        // assert
        Assert.Equal([true, false, false], layout.Select(c => c.Visible));
    }

    [Fact]
    public void Plan_Should_NeverDropAColumnWithoutDropPriority_When_BudgetIsExtremelyNarrow()
    {
        // arrange
        int[] widths = [4, 4, 3];

        // act
        var layout = TableLayout.Plan(budget: 1, s_columns, widths);

        // assert
        Assert.True(layout[0].Visible);
    }

    [Fact]
    public void Plan_Should_TruncateTheFirstVisibleColumn_When_EvenNeverDroppedColumnsDoNotFit()
    {
        // arrange
        int[] widths = [4, 4, 3];

        // act
        var layout = TableLayout.Plan(budget: 1, s_columns, widths);

        // assert
        Assert.Equal(1, layout[0].Width);
    }

    [Fact]
    public void RenderRow_Should_JoinVisibleCellsWithGutter_When_AllColumnsFit()
    {
        // arrange
        var layout = TableLayout.Plan(budget: 19, s_columns, [4, 4, 3]);
        TableCellSpec[] cells = [new("Ann", "bold"), new("Lead", ""), new("2m", "")];

        // act
        var line = TableRenderer.RenderRow("> ", new TableCellSpec("*"), cells, s_columns, layout);

        // assert
        line.MatchInlineSnapshot("> * [bold]Ann [/]    Lead     2m");
    }

    [Fact]
    public void RenderRow_Should_SkipHiddenColumns_When_LayoutMarksThemInvisible()
    {
        // arrange
        var layout = TableLayout.Plan(budget: 5, s_columns, [4, 4, 3]);
        TableCellSpec[] cells = [new("Ann"), new("Lead"), new("2m")];

        // act
        var line = TableRenderer.RenderRow("  ", new TableCellSpec(" "), cells, s_columns, layout);

        // assert
        line.MatchInlineSnapshot("    Ann ");
    }

    [Fact]
    public void RenderRow_Should_RightAlignAndPad_When_ColumnAlignmentIsRight()
    {
        // arrange
        var layout = TableLayout.Plan(budget: 19, s_columns, [4, 4, 3]);
        TableCellSpec[] cells = [new("Ann"), new("Lead"), new("2m")];

        // act
        var line = TableRenderer.RenderRow("  ", new TableCellSpec(" "), cells, s_columns, layout);

        // assert
        Assert.EndsWith(" 2m", line);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void RenderRule_Should_ReturnEmpty_When_WidthIsNotPositive(int width)
    {
        // act
        var rule = TableRenderer.RenderRule(width, "grey");

        // assert
        Assert.Equal(string.Empty, rule);
    }

    [Fact]
    public void RenderRule_Should_FillWidthWithRuleGlyphs_When_WidthIsPositive()
    {
        // act
        var rule = TableRenderer.RenderRule(5, "grey");

        // assert
        rule.MatchInlineSnapshot("[grey]─────[/]");
    }
}
