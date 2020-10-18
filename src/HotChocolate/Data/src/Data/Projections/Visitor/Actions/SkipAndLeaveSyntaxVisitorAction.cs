namespace HotChocolate.Data.Projections
{
    public class SkipAndLeaveSelectionVisitorAction : ISelectionVisitorAction
    {
        public SelectionVisitorActionKind Kind => SelectionVisitorActionKind.SkipAndLeave;
    }
}
