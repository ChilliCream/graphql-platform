using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Resolvers;
using HotChocolate.Runtime;

namespace HotChocolate.Configuration
{
    internal partial class SchemaConfiguration
    {
        private Dictionary<Type, CustomContextDescriptor> _customContexts =
            new Dictionary<Type, CustomContextDescriptor>();

        internal IReadOnlyCollection<CustomContextDescriptor>
            CustomContextDescriptors => _customContexts.Values;

        public void RegisterCustomContext<T>(
            ExecutionScope scope,
            Func<IServiceProvider, T> contextFactory = null)
        {
            Func<IServiceProvider, object> factory = null;
            if (contextFactory != null)
            {
                factory = new Func<IServiceProvider, object>(
                    sp => contextFactory(sp));
            }

            var descriptor = new CustomContextDescriptor(
                typeof(T), factory, scope);
            _customContexts[typeof(T)] = descriptor;
        }
    }
}
