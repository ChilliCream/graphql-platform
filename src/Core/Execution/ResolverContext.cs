using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
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
        // todo: remove
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
                resolverTask.FieldSelection, executionContext.Variables);
        }

        public ISchema Schema => _executionContext.Schema;

        public ObjectType ObjectType => _resolverTask.ObjectType;

        public ObjectField Field => _resolverTask.FieldSelection.Field;

        public DocumentNode QueryDocument => _executionContext.QueryDocument;

        public OperationDefinitionNode Operation => _executionContext.Operation;

        public FieldNode FieldSelection => _resolverTask.FieldSelection.Selection;

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

            if (argumentValue.Value == null)
            {
                return default;
            }

            if (TryConvertValue(argumentValue, out value))
            {
                return value;
            }

            throw new QueryException(
               new FieldError(
                    $"Could not convert argument {name} from " +
                    $"{argumentValue.ClrType.FullName} to " +
                    $"{typeof(T).FullName}.",
                    _resolverTask.FieldSelection.Selection));
        }

        private bool TryConvertValue<T>(ArgumentValue argumentValue, out T value)
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
            => ReportError(new FieldError(errorMessage, FieldSelection));

        public void ReportError(IQueryError error)
            => _executionContext.ReportError(error);

        public IDirective Directive(string name)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IDirective> Directives(DirectiveScope scope)
        {
            throw new NotImplementedException();
        }
    }

    internal readonly struct DirectiveContext
        : IDirectiveContext
    {
        public IDirective Directive => throw new NotImplementedException();

        public T Argument<T>(string name)
        {
            throw new NotImplementedException();
        }

        public IReadOnlyCollection<FieldSelection> CollectFields()
        {
            throw new NotImplementedException();
        }

        public Task<T> ResolveFieldAsync<T>()
        {
            throw new NotImplementedException();
        }
    }
}
