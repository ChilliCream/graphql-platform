using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using HotChocolate.Execution;
using HotChocolate.Execution.ValueConverters;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Resolvers
{
    internal class ResolverContext
        : IResolverContext
    {
        private static readonly List<IInputValueConverter> _converters =
            new List<IInputValueConverter>
            {
                new ListValueConverter(),
                new FloatValueConverter(),
                new DateTimeValueConverter()
            };
        private static readonly ArgumentResolver _argumentResolver =
            new ArgumentResolver();
        private readonly ExecutionContext _executionContext;
        private readonly FieldResolverTask _fieldResolverTask;
        private Dictionary<string, ArgumentValue> _arguments;

        public ResolverContext(
            ExecutionContext executionContext,
            FieldResolverTask fieldResolverTask)
        {
            if (executionContext == null)
            {
                throw new ArgumentNullException(nameof(executionContext));
            }

            if (fieldResolverTask == null)
            {
                throw new ArgumentNullException(nameof(fieldResolverTask));
            }

            _executionContext = executionContext;
            _fieldResolverTask = fieldResolverTask;
            _arguments = _argumentResolver.CoerceArgumentValues(
                fieldResolverTask.ObjectType, fieldResolverTask.FieldSelection,
                executionContext.Variables);
        }

        public Schema Schema => _executionContext.Schema;

        public ObjectType ObjectType => _fieldResolverTask.ObjectType;

        public Field Field => _fieldResolverTask.FieldSelection.Field;

        public DocumentNode QueryDocument => _executionContext.QueryDocument;

        public OperationDefinitionNode Operation => _executionContext.Operation;

        public FieldNode FieldSelection => _fieldResolverTask.FieldSelection.Node;

        public ImmutableStack<object> Source => _fieldResolverTask.Source;

        public Path Path => _fieldResolverTask.Path;

        public T Argument<T>(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (_arguments.TryGetValue(name, out ArgumentValue argumentValue))
            {
                if (argumentValue.Value is T v
                    || TryConvertValue(argumentValue, out v))
                {
                    return v;
                }

                throw new QueryException(
                    $"Could not convert argument {name} from " +
                    $"{argumentValue.NativeType.FullName} to " +
                    $"{typeof(T).FullName}.");
            }

            return default(T);
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
            return (T)_executionContext.GetService(typeof(T));
        }
    }
}
