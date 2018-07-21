using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using HotChocolate.Execution.ValueConverters;
using HotChocolate.Internal;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    internal readonly struct ResolverContext
        : IResolverContext
    {
        // remove
        private static readonly List<IInputValueConverter> _converters =
            new List<IInputValueConverter>
            {
                new ListValueConverter(),
                new FloatValueConverter(),
                new DateTimeValueConverter()
            };
        private static readonly ArgumentResolver _argumentResolver =
            new ArgumentResolver();
        private readonly IExecutionContext _executionContext;
        private readonly ResolverTask _resolverTask;
        private readonly Dictionary<string, ArgumentValue> _arguments;

        public ResolverContext(
            IExecutionContext executionContext,
            ResolverTask resolverTask)
        {
            if (executionContext == null)
            {
                throw new ArgumentNullException(nameof(executionContext));
            }

            if (resolverTask == null)
            {
                throw new ArgumentNullException(nameof(resolverTask));
            }

            _executionContext = executionContext;
            _resolverTask = resolverTask;
            _arguments = _argumentResolver.CoerceArgumentValues(
                resolverTask.ObjectType, resolverTask.FieldSelection,
                executionContext.Variables);
        }

        public ISchema Schema => _executionContext.Schema;

        public ObjectType ObjectType => _resolverTask.ObjectType;

        public ObjectField Field => _resolverTask.FieldSelection.Field;

        public DocumentNode QueryDocument => _executionContext.QueryDocument;

        public OperationDefinitionNode Operation => _executionContext.Operation;

        public FieldNode FieldSelection => _resolverTask.FieldSelection.Node;

        public ImmutableStack<object> Source => _resolverTask.Source;

        public Path Path => _resolverTask.Path;

        public T Argument<T>(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (_arguments.TryGetValue(name, out ArgumentValue argumentValue))
            {
                return ConvertArgumentValue<T>(name, argumentValue);
            }

            return default(T);
        }

        private T ConvertArgumentValue<T>(string name, ArgumentValue argumentValue)
        {
            if (argumentValue.Value is T value)
            {
                return value;
            }

            Type type = typeof(T);
            if (argumentValue.Value == null)
            {
                return default(T);
            }

            if (TryConvertValue(argumentValue, out value))
            {
                return value;
            }

            throw new QueryException(
               new FieldError(
                   $"Could not convert argument {name} from " +
                   $"{argumentValue.NativeType.FullName} to " +
                   $"{typeof(T).FullName}.",
                   _resolverTask.FieldSelection.Node));
        }

        private bool TryConvertValue<T>(ArgumentValue argumentValue, out T value)
        {
            foreach (IInputValueConverter converter in _converters
                .Where(t => t.CanConvert(argumentValue.Type)))
            {
                if (converter.TryConvert(argumentValue.NativeType, typeof(T),
                    argumentValue.Value, out object cv))
                {
                    value = (T)cv;
                    return true;
                }
            }

            value = default(T);
            return false;
        }

        public T Parent<T>()
        {
            return (T)Source.Peek();
        }

        public T Service<T>()
        {
            return (T)_executionContext.Services.GetService(typeof(T));
        }

        public T State<T>()
        {
            throw new NotImplementedException();
        }

        public T Loader<T>(string key)
        {
            return _executionContext.DataLoaders.GetDataLoader<T>(key);
        }
    }
}
