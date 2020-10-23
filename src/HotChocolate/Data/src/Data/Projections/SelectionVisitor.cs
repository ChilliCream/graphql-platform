namespace HotChocolate.Data.Projections
{
    public class SelectionVisitor
    {
        /// <summary>
        /// The visitor default action.
        /// </summary>
        /// <value></value>
        protected virtual ISelectionVisitorAction DefaultAction { get; } = Continue;

        /// <summary>
        /// Ends traversing the graph.
        /// </summary>
        public static ISelectionVisitorAction Break { get; } = new BreakSelectionVisitorAction();

        /// <summary>
        /// Skips the child nodes and the current node.
        /// </summary>
        public static ISelectionVisitorAction Skip { get; } = new SkipSelectionVisitorAction();

        /// <summary>
        /// Continues traversing the graph.
        /// </summary>
        public static ISelectionVisitorAction Continue { get; } =
            new ContinueSelectionVisitorAction();

        /// <summary>
        /// Skips the child node but completes the current node.
        /// </summary>
        public static ISelectionVisitorAction SkipAndLeave { get; } =
            new SkipAndLeaveSelectionVisitorAction();
    }
}
