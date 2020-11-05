namespace HotChocolate.Data.Projections
{
    public class BreakSelectionVisitorAction : ISelectionVisitorAction
    {
        public SelectionVisitorActionKind Kind => SelectionVisitorActionKind.Break;
    }
}
