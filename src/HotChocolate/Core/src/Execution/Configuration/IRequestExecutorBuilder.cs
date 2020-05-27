using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution.Configuration
{
    public interface IRequestExecutorBuilder
    {
        string Name { get; }

        IServiceCollection Services { get; }
    }
}