namespace HotChocolate.Data.Projections
{
    /// <summary>
    /// This descriptor is used to configure a <see cref="ProjectionConvention"/>.
    /// </summary>
    public interface IProjectionConventionDescriptor
    {
        IProjectionConventionDescriptor RegisterFieldHandler<THandler>()
            where THandler : IProjectionFieldHandler;

        IProjectionConventionDescriptor RegisterFieldHandler<THandler>(THandler handler)
            where THandler : IProjectionFieldHandler;
    }
}
