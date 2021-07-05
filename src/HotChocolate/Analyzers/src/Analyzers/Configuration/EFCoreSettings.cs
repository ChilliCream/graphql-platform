namespace HotChocolate.Analyzers.Configuration
{
    /// <summary>
    /// The Neo4J generator settings.
    /// </summary>
    public class EFCoreSettings
    {
        /// <summary>
        /// Gets the name.
        /// </summary>
        public string Name { get; set; } = "EFCore";

        /// <summary>
        /// Gets or sets the namespace.
        /// </summary>
        public string? Namespace { get; set; }
    }
}
