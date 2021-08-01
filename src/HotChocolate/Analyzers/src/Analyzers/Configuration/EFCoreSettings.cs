namespace HotChocolate.Analyzers.Configuration
{
    /// <summary>
    /// The Entity Framework generator settings.
    /// </summary>
    public class EFCoreSettings
    {
        /// <summary>
        /// The name.
        /// </summary>
        public string Name { get; set; } = "EFCore";

        /// <summary>
        /// The database name.
        /// </summary>
        public string DatabaseName { get; set; } = "EFCore";

        /// <summary>
        /// The namespace to use for generated code.
        /// </summary>
        public string? Namespace { get; set; }
    }
}
