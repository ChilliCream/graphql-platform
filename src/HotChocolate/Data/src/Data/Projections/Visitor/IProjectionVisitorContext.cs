namespace HotChocolate.Data.Projections;

public interface IProjectionVisitorContext
    : ISelectionVisitorContext
{
    IList<IError> Errors { get; }
}
