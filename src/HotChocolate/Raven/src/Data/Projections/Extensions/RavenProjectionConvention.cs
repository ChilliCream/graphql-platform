using HotChocolate.Data.Projections;
using HotChocolate.Data.Raven.Projections;

namespace HotChocolate.Data;

internal sealed class RavenProjectionConvention : ProjectionConvention
{
    protected override void Configure(IProjectionConventionDescriptor descriptor)
    {
        descriptor.Provider<RavenQueryableProjectionProvider>();
    }
}
