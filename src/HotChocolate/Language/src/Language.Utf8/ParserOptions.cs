namespace HotChocolate.Language
{
    /// <summary>
    /// The GraphQL parser options.
    /// </summary>
    public sealed class ParserOptions
    {
        /// <summary>
        /// Initializes a new instance of <see cref="ParserOptions"/>.
        /// </summary>
        /// <param name="noLocations">
        /// Defines that the parse shall not preserve syntax node locations.
        /// </param>
        /// <param name="allowFragmentVariables">
        /// Defines that the parser shall parse fragment variables.
        /// </param>
        public ParserOptions(
            bool noLocations = false,
            bool allowFragmentVariables = false)
        {
            NoLocations = noLocations;
            Experimental = new ParserOptionsExperimental(
                allowFragmentVariables);
        }

        /// <summary>
        /// By default, the parser creates <see cref="ISyntaxNode" />s
        /// that know the location in the source that they correspond to.
        /// This configuration flag disables that behavior
        /// for performance or testing.
        /// </summary>
        public bool NoLocations { get; }

        /// <summary>
        /// Gets the experimental parser options
        /// which are by default switched of.
        /// </summary>
        public ParserOptionsExperimental Experimental { get; }

        /// <summary>
        /// Gets the default parser options.
        /// </summary>
        public static ParserOptions Default { get; } = new();

        /// <summary>
        /// Gets the default parser options with the locations switched of.
        /// </summary>
        public static ParserOptions NoLocation { get; } = new(noLocations: true);
    }
}
