using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace HotChocolate.Execution
{
    internal partial class MiddlewareContext : IMiddlewareContext
    {
        private object? _result;
        private object? _parent;

        public Path Path { get; private set; } = default!;

        public IImmutableDictionary<string, object?> ScopedContextData { get; set; } = default!;

        public IImmutableDictionary<string, object?> LocalContextData { get; set; } = default!;

        public IType? ValueType { get; set; }

        public ResultMap ResultMap { get; private set; } = default!;

        public bool HasErrors { get; private set; }

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
        public T Source<T>()
        {
            if (_parent is null)
            {
                return default;
            }

            if (_parent is T casted)
            {
                return casted;
            }

            if (_operationContext.Converter.TryConvert<object, T>(_parent, out casted))
            {
                return casted;
            }

            throw new InvalidCastException(
                $"The parent cannot be casted to {typeof(T).FullName}.");
        }

        [return: MaybeNull]
        public T Parent<T>() => Source<T>();
    }
}
