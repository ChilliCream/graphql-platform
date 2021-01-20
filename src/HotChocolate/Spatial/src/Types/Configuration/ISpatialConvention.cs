using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Spatial.Transformation;

namespace HotChocolate.Types.Spatial.Configuration
{
    /// <summary>
    /// A conventions that configures the behaviour of spatial types
    /// </summary>
    public interface ISpatialConvention : IConvention
    {
        /// <summary>
        /// The default SRID/CRS of the schema. All incoming queries will be translated to this SRID
        /// </summary>
        int DefaultSrid { get; }

        /// <summary>
        /// The <see cref="IGeometryTransformerFactory"/> for this convention.
        /// </summary>
        IGeometryTransformerFactory TransformerFactory { get; }
    }
}
