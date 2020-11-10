namespace HotChocolate.Data.Projections
{
    public interface ISelectionVisitorAction
    {
        SelectionVisitorActionKind Kind { get; }
    }
}
