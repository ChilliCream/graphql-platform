namespace HotChocolate.Data.Projections.Expressions.Configuration;

internal class QueryableProjectionConventionDescriptor
    : ProjectionConventionDescriptorProxy
    , IQueryableProjectionConventionDescriptor
{
    public QueryableProjectionConventionDescriptor(IProjectionConventionDescriptor descriptor) :
        base(descriptor)
    {
    }
}
