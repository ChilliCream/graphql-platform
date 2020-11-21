namespace HotChocolate.Data.Neo4J.Language
{
    public interface IExposesProperties<T>
    {
        /**
         * Creates a a copy of this property container with additional properties.
         * Creates a property container without properties when no properties are passed to this method.
         *
         * @param newProperties the new properties (can be {@literal null} to remove exiting properties).
         * @return The new property container.
         */
        /// <summary>
        /// Creates a a copy of this property container with additional properties.
         * Creates a property container without properties when no properties are passed to this method.
        /// </summary>
        /// <param name="newProperties"></param>
        /// <returns></returns>
        T withProperties(MapExpression newProperties);

        /**
         * Creates a a copy of this property container with additional properties.
         * Creates a property container without properties when no properties are passed to this method.
         *
         * @param keysAndValues A list of key and values. Must be an even number, with alternating {@link String} and {@link Expression}.
         * @return The new property container.
         */
        T withProperties(Object...keysAndValues);
    }
}