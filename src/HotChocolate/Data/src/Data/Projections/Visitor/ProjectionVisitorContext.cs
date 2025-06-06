using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Data.Projections;

public abstract class ProjectionVisitorContext<T>
    : SelectionVisitorContext,
      IProjectionVisitorContext<T>
{
    protected ProjectionVisitorContext(
        IResolverContext context,
        IOutputType initialType,
        ProjectionScope<T> projectionScope) : base(context)
    {
        ArgumentNullException.ThrowIfNull(initialType);

        Types.Push(initialType);
        Scopes = new Stack<ProjectionScope<T>>();
        Scopes.Push(projectionScope);
    }

    public Stack<ProjectionScope<T>> Scopes { get; }

    public Stack<IType> Types { get; } = new Stack<IType>();

    public IList<IError> Errors { get; } = [];
}
