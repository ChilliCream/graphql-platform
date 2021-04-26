namespace HotChocolate.Types
{
    /// <summary>
    /// The settings needed to apply default costs to resolvers.
    /// </summary>
    public interface ICostSettings
    {
        /// <summary>
        /// Defines if default cost and multipliers shall be applied to the schema.
        /// </summary>
        bool ApplyDefaults { get; }

        /// <summary>
        /// Gets or sets the complexity that is applied to all fields
        /// that do not have a cost directive.
        /// </summary>
        int DefaultComplexity { get; }

        /// <summary>
        /// Gets or sets the complexity that is applied to async and data
        /// resolvers if <see cref="ApplyDefaults"/> is <c>true</c>.
        /// </summary>
        int DefaultResolverComplexity { get; }
    }
}
