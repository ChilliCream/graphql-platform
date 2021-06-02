namespace HotChocolate.Analyzers.Configuration
{

    /// <summary>
    /// The Neo4J generator settings.
    /// </summary>
    public class Neo4JSettings
    {
        /// <summary>
        /// Gets the Neo4J database name.
        /// </summary>
        /// <value></value>
        public string DatabaseName { get; set; } = default!;

        /// <summary>
        /// Gets or sets the namespace.
        /// </summary>
        public string? Namespace { get; set; }
    }
}
