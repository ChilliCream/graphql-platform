namespace HotChocolate.Language
{
    public sealed class ParserOptions
    {
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

        public static ParserOptions Default { get; } = new ParserOptions();

        public static ParserOptions NoLocation { get; } =
            new ParserOptions(noLocations: true);
    }
}
