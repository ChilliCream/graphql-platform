namespace HotChocolate.Data.Projections
{
    public class ContinueSelectionVisitorAction : ISelectionVisitorAction
    {
        public SelectionVisitorActionKind Kind => SelectionVisitorActionKind.Continue;
    }
}
