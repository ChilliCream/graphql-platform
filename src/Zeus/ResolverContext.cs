using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Zeus.Execution
{
    public class ResolverContext
        : IResolverContext
    {
        public ResolverContext(IImmutableStack<object> path)
        {
            Path = path;
        }

        public IImmutableStack<object> Path { get; }
        internal IDictionary<string, object> Values { get; set; }

        public T Argument<T>(string name)
        {
            throw new NotImplementedException();
        }

        public object GetService(Type serviceType)
        {
            throw new NotImplementedException();
        }

        public T Parent<T>() => (T)Path.Peek();
    }
}