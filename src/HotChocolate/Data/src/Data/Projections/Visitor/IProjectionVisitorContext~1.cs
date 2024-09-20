namespace HotChocolate.Data.Projections;

public interface IProjectionVisitorContext<T>
    : IProjectionVisitorContext
{
    Stack<ProjectionScope<T>> Scopes { get; }
}
