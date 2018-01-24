using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Zeus.Resolvers
{
    public class ResolverContext
        : IResolverContext
    {
        private static Dictionary<string, object> _empty = new Dictionary<string, object>();
        private readonly IServiceProvider _serviceProvider;
        private readonly IDictionary<string, object> _arguments;

        private ResolverContext(IServiceProvider serviceProvider,
            ISchema schema,
            IDictionary<string, object> arguments,
            IImmutableStack<object> path)
        {
            _serviceProvider = serviceProvider;
            _arguments = arguments;
            Schema = schema;
            Path = path;
        }

        public ISchema Schema { get; }

        public IImmutableStack<object> Path { get; }

        public T Parent<T>() => (T)Path.Peek();

        public T Argument<T>(string name)
        {
            if (_arguments.TryGetValue(name, out object o))
            {
                return (T)o;
            }
            throw new ArgumentException(
                "The specified argument does not exist.",
                nameof(name));
        }

        public T Service<T>()
        {
            return (T)_serviceProvider.GetService(typeof(T));
        }

        public IResolverContext Copy(object newParent)
        {
            return Copy(_empty, newParent);
        }

        public IResolverContext Copy(IDictionary<string, object> arguments, object newParent)
        {
            if (arguments == null)
            {
                throw new ArgumentNullException(nameof(arguments));
            }

            if (newParent == null)
            {
                throw new ArgumentNullException(nameof(newParent));
            }

            return new ResolverContext(_serviceProvider, Schema, arguments, Path.Push(newParent));
        }

        #region Factories

        public static ResolverContext Create(
            IServiceProvider serviceProvider,
            ISchema schema,
            IDictionary<string, object> arguments,
            object root)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            if (arguments == null)
            {
                throw new ArgumentNullException(nameof(arguments));
            }

            IImmutableStack<object> path = ImmutableStack<object>.Empty;
            if (root != null)
            {
                path = path.Push(root);
            }
            return new ResolverContext(serviceProvider, schema, arguments, path);
        }

        #endregion
    }
}