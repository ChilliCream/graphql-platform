using HotChocolate.Data.Projections;

namespace HotChocolate.Data.MongoDb.Projections;

/// <summary>
/// A specific representation of <see cref="IProjectionConventionDescriptor"/>
/// for filtering on mongo
/// </summary>
public interface IMongoProjectionConventionDescriptor : IProjectionConventionDescriptor
{
}
