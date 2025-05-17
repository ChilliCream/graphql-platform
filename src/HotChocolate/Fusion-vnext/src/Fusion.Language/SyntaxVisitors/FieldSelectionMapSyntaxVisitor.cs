namespace HotChocolate.Fusion.Language;

public abstract class FieldSelectionMapSyntaxVisitor
{
    /// <summary>
    /// Ends traversing the graph.
    /// </summary>
    public static ISyntaxVisitorAction Break { get; }
        = new BreakSyntaxVisitorAction();

    /// <summary>
    /// Skips the child nodes and the current node.
    /// </summary>
    public static ISyntaxVisitorAction Skip { get; }
        = new SkipSyntaxVisitorAction();

    /// <summary>
    /// Continues traversing the graph.
    /// </summary>
    public static ISyntaxVisitorAction Continue { get; }
        = new ContinueSyntaxVisitorAction();

    /// <summary>
    /// Skips the child node but completes the current node.
    /// </summary>
    public static ISyntaxVisitorAction SkipAndLeave { get; }
        = new SkipAndLeaveSyntaxVisitorAction();
}
