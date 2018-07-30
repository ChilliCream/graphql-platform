using System;

namespace HotChocolate.Runtime
{
    public class CustomContextDescriptor
        : IScopedStateDescriptor<Type>
    {
        public CustomContextDescriptor(
            Type type,
            Func<IServiceProvider, object> factory,
            ExecutionScope scope)
        {
            Type = type
                ?? throw new ArgumentNullException(nameof(type));
            Factory = factory;
            Scope = scope;
        }

        public Type Key => Type;

        public Type Type { get; }

        public Func<IServiceProvider, object> Factory { get; }

        public ExecutionScope Scope { get; }
    }
}
