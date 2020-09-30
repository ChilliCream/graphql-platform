namespace HotChocolate.Data.Projections
{
    public interface IProjectionFieldHandler<in TContext, T>
        : IProjectionFieldHandler<TContext>
        where TContext : ProjectionVisitorContext<T>
    {
    }
}
