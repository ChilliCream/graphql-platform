using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution.Configuration
{
    public interface IRequestExecutorBuilder
    {
        NameString Name { get; }

        IServiceCollection Services { get; }
    }
}