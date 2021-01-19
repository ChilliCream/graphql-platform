using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Spatial.Transformation;

namespace HotChocolate.Types.Spatial.Configuration
{
    public interface ISpatialConvention : IConvention
    {
        int DefaultSrid { get; }

        IGeometryProjectorFactory ProjectorFactory { get; }
    }
}
