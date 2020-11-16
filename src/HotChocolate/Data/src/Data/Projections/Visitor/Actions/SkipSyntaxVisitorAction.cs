namespace HotChocolate.Data.Projections
{
    public class SkipSelectionVisitorAction : ISelectionVisitorAction
    {
        public SelectionVisitorActionKind Kind => SelectionVisitorActionKind.Skip;
    }
}
