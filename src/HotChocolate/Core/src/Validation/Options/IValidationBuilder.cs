using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Validation.Options
{
    /// <summary>
    /// A builder for configuring the max complexity rule.
    /// </summary>
    public interface IValidationBuilder
    {
        /// <summary>
        /// Gets the name of the schema for which this rule is configure.
        /// </summary>
        NameString Name { get; }

        /// <summary>
        /// Gets the application service collection.
        /// </summary>
        IServiceCollection Services { get; }
    }
}
