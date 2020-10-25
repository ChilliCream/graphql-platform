using Microsoft.Extensions.Options;

namespace HotChocolate.Execution.Configuration
{
    /// <summary>
    /// Represents something that configures the <see cref="RequestExecutorFactoryOptions"/>.
    /// </summary>
    public interface INamedRequestExecutorFactoryOptions
        : IConfigureOptions<RequestExecutorFactoryOptions>
    {
        /// <summary>
        /// The schema name to which this instance provides configurations to.
        /// </summary>
        NameString SchemaName { get; }
    }
}
