using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using HotChocolate.Utilities;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution
{
    internal sealed class ResolverContext
        : IResolverContext
    {
        private readonly IExecutionContext _executionContext;
        private readonly ResolverTask _resolverTask;
        private readonly Dictionary<string, ArgumentValue> _arguments;
        private readonly ITypeConversion _converter;

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

            _converter = _executionContext.Services.GetTypeConversion();
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

        public IImmutableStack<object> Source => _resolverTask.Source;

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
                return CoerceArgumentValue<T>(name, argumentValue);
            }

            return default(T);
        }

        private T CoerceArgumentValue<T>(
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

            // TODO : Resources
            throw new QueryException(
               QueryError.CreateFieldError(
                    $"Could not convert argument {name} from " +
                    $"{argumentValue.Type.ClrType.FullName} to " +
                    $"{typeof(T).FullName}.",
                    _resolverTask.Path,
                    _resolverTask.FieldSelection.Selection));
        }

        private bool TryConvertValue<T>(
            ArgumentValue argumentValue,
            out T value)
        {
            if (_converter.TryConvert(
                argumentValue.Type.ClrType, typeof(T),
                argumentValue.Value, out object converted))
            {
                value = (T)converted;
                return true;
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
            if (typeof(T) == typeof(IServiceProvider))
            {
                return (T)_executionContext.Services;
            }
            return (T)_executionContext.Services.GetRequiredService(typeof(T));
        }

        public object Service(Type service) =>
            _executionContext.Services.GetRequiredService(service);

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

            // TODO : Resources
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
