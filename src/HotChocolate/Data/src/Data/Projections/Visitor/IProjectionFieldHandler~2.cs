namespace HotChocolate.Data.Projections
{
    // TODO obsolete
    public interface IProjectionFieldHandler<TContext, T>
        : IProjectionFieldHandler<TContext>
        where TContext : ProjectionVisitorContext<T>
    {
    }
}
