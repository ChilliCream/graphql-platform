using System;
using System.Collections.Immutable;
using HotChocolate.Resolvers;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace HotChocolate.Execution
{
    internal partial class MiddlewareContext : IMiddlewareContext
    {
        private object? _result;
        private object? _parent;

        // todo: we should deprecate this one
        public IImmutableStack<object?> Source { get; private set; } = default!;

        public Path Path { get; private set; } = default!;

        public IImmutableDictionary<string, object?> ScopedContextData { get; set; } = default!;

        public IImmutableDictionary<string, object?> LocalContextData { get; set; } = default!;

        internal Task? Task { get; set; }

        public object? Result
        {
            get => _result;
            set
            {
                _result = value;
                IsResultModified = true;
            }
        }

        public bool IsResultModified { get; private set; }

        public void ModifyScopedContext(ModifyScopedContext modify)
        {
            ScopedContextData = modify(ScopedContextData);
        }

        [return: MaybeNull]
        public T Parent<T>()
        {
            if (_parent is null)
            {
                return default;
            }

            if (_parent is T casted)
            {
                return casted;
            }

            throw new InvalidCastException(
                $"The parent cannot be casted to {typeof(T).FullName}.");
        }
    }
}
