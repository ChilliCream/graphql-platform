using System;

namespace HotChocolate.Execution
{
    public sealed class RequestExecutorEvictedEventArgs : EventArgs
    {
        public RequestExecutorEvictedEventArgs(string name, IRequestExecutor evictedExecutor)
        {
            Name = name;
            EvictedExecutor = evictedExecutor;
        }

        public string Name { get; }

        public IRequestExecutor EvictedExecutor { get; }
    }
}