using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using HotChocolate.Execution.ValueConverters;
using HotChocolate.Utilities;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using System.Threading;

namespace HotChocolate.Execution
{
    internal sealed class ResolverContext
        : IResolverContext
    {
        // todo: remove
        private static readonly List<IInputValueConverter> _converters =
            new List<IInputValueConverter>
            {
                new ListValueConverter(),
                new FloatValueConverter(),
                new DateTimeValueConverter()
            };
        private readonly IExecutionContext _executionContext;
        private readonly ResolverTask _resolverTask;
        private readonly Dictionary<string, ArgumentValue> _arguments;

        public ResolverContext(
            IExecutionContext executionContext,
            ResolverTask resolverTask,
            CancellationToken requestAborted)
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
            RequestAborted = requestAborted;

            _arguments = resolverTask.FieldSelection
                .CoerceArgumentValues(executionContext.Variables);
        }

        public ISchema Schema => _executionContext.Schema;

        public ObjectType ObjectType => _resolverTask.ObjectType;

        public ObjectField Field => _resolverTask.FieldSelection.Field;

        public DocumentNode QueryDocument => _executionContext.QueryDocument;

        public OperationDefinitionNode Operation => _executionContext.Operation;

        public FieldNode FieldSelection =>
            _resolverTask.FieldSelection.Selection;

        public ImmutableStack<object> Source => _resolverTask.Source;

        public Path Path => _resolverTask.Path;

        public CancellationToken CancellationToken => RequestAborted;

        public CancellationToken RequestAborted { get; }

        public T Argument<T>(NameString name)
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

        private T ConvertArgumentValue<T>(
            string name,
            ArgumentValue argumentValue)
        {
            if (argumentValue.Value is T value)
            {
                return value;
            }

            if (argumentValue.Value == null)
            {
                return default;
            }

            if (TryConvertValue(argumentValue, out value))
            {
                return value;
            }

            throw new QueryException(
               QueryError.CreateFieldError(
                    $"Could not convert argument {name} from " +
                    $"{argumentValue.ClrType.FullName} to " +
                    $"{typeof(T).FullName}.",
                    _resolverTask.Path,
                    _resolverTask.FieldSelection.Selection));
        }

        private static bool TryConvertValue<T>(
            ArgumentValue argumentValue,
            out T value)
        {
            foreach (IInputValueConverter converter in _converters
                .Where(t => t.CanConvert(argumentValue.Type)))
            {
                if (converter.TryConvert(argumentValue.ClrType, typeof(T),
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

        public T CustomContext<T>()
        {
            if (_executionContext.CustomContexts == null)
            {
                return default;
            }
            return _executionContext.CustomContexts.GetCustomContext<T>();
        }

        public T CustomProperty<T>(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (_executionContext.RequestProperties
                .TryGetValue(key, out object value) && value is T v)
            {
                return v;
            }

            throw new ArgumentException(
                "The specified property does not exist.");
        }

        public T DataLoader<T>(string key)
        {
            if (_executionContext.DataLoaders == null)
            {
                return default;
            }
            return _executionContext.DataLoaders.GetDataLoader<T>(key);
        }

        public T Resolver<T>()
        {
            return _executionContext.GetResolver<T>();
        }

        public void ReportError(string errorMessage)
            => ReportError(QueryError.CreateFieldError(
                    errorMessage, Path, FieldSelection));

        public void ReportError(IError error)
            => _executionContext.ReportError(error);
    }
}
