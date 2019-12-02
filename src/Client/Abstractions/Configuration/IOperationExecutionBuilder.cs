using System;

namespace StrawberryShake.Configuration
{
    public interface IOperationExecutionBuilder
    {
        void AddConfiguration(IOperationExecutionConfiguration configuration);

        IOperationExecutorFactory Build(IServiceProvider services);
    }
}
