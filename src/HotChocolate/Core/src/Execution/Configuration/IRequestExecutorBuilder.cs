using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution.Configuration
{
    public interface IRequestExecutorBuilder
    {
        /// <summary>
        /// Gets the name of the schema.
        /// </summary>
        NameString Name { get; }

        /// <summary>
        /// Gets the application services.
        /// </summary>
        IServiceCollection Services { get; }
    }
}