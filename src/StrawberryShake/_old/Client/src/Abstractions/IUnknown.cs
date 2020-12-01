namespace StrawberryShake
{
    /// <summary>
    /// Represents an unknown schema type a indicates that the schema has changed
    /// since the last time the application was built.
    /// </summary>
    public interface IUnknown
    {
        /// <summary>
        /// Gets the GraphQL type name.
        /// </summary>
        string TypeName { get; }
    }
}
