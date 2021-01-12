namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// A container having properties. A property container must be {INamed} with a non empty name to
    /// be able to refer to properties.
    /// </summary>
    public interface IPropertyContainer : INamed
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="name"></param>
        /// <returns>A new Property associated with Named </returns>
        public Property Property(string name);
    }
}
