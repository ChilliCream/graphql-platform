namespace HotChocolate.Types.Spatial.Transformation
{
    /// <summary>
    /// Provides instances of <see cref="IGeometryTransformer"/>
    /// </summary>
    public interface IGeometryTransformerFactory
    {
        /// <summary>
        /// Creates a <see cref="IGeometryTransformer"/> between to SRID
        /// </summary>
        /// <param name="fromSrid">the SRID to convert from</param>
        /// <param name="toSrid">the SRID to convert to</param>
        /// <returns>
        /// A transformer that converts from <paramref name="fromSrid"/> to
        /// <paramref name="toSrid"/>
        /// </returns>
        IGeometryTransformer Create(int fromSrid, int toSrid);

        /// <summary>
        /// Checks if any coordinate systems have been registered on the factory
        /// </summary>
        /// <returns><c>true</c> when there is at least one registered</returns>
        bool HasCoordinateSystems();

        /// <summary>
        /// Checks if a coordinate system is registered on the factory
        /// </summary>
        /// <param name="srid">The srid to look for</param>
        /// <returns><c>true</c> if a coordinate system is registered</returns>
        bool ContainsCoordinateSystem(int srid);
    }
}
