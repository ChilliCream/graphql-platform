using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using HotChocolate.Utilities;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;
using HotChocolate.Properties;

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
            _arguments = resolverTask.FieldSelection.CoerceArgumentValues(
                executionContext.Variables, resolverTask.Path);
        }

        public ISchema Schema =>
            _executionContext.Schema;

        public ObjectType ObjectType =>
            _resolverTask.ObjectType;

        public ObjectField Field =>
            _resolverTask.FieldSelection.Field;

        public DocumentNode Document =>
            _executionContext.Operation.Document;

        public DocumentNode QueryDocument => Document;

        public OperationDefinitionNode Operation =>
            _executionContext.Operation.Definition;

        public FieldNode FieldSelection =>
            _resolverTask.FieldSelection.Selection;

        public IImmutableStack<object> Source => _resolverTask.Source;

        public Path Path => _resolverTask.Path;

        public CancellationToken CancellationToken => RequestAborted;

        public CancellationToken RequestAborted { get; }

        public IDictionary<string, object> ContextData =>
            _executionContext.ContextData;

        public IImmutableDictionary<string, object> ScopedContextData
        {
            get => _resolverTask.ScopedContextData;
            set => _resolverTask.ScopedContextData = value;
        }

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

            return default;
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

            IError error = ErrorBuilder.New()
                .SetMessage(string.Format(
                    CultureInfo.InvariantCulture,
                    CoreResources.ResolverContext_ArgumentConversion,
                    name,
                    argumentValue.Type.ClrType.FullName,
                    typeof(T).FullName))
                .SetPath(_resolverTask.Path)
                .AddLocation(_resolverTask.FieldSelection.Selection)
                .Build();

            throw new QueryException(error);
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

            value = default;
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

        public T CustomProperty<T>(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (ContextData.TryGetValue(key, out object value))
            {
                if (value is null)
                {
                    return default;
                }

                if (value is T v)
                {
                    return v;
                }
            }

            throw new ArgumentException(
                CoreResources.ResolverContext_CustomPropertyNotExists);
        }

        public T Resolver<T>()
        {
            return _executionContext.Activator.GetOrCreateResolver<T>();
        }

        public void ReportError(string errorMessage)
            => ReportError(ErrorBuilder.New()
                .SetMessage(errorMessage)
                .SetPath(Path)
                .AddLocation(FieldSelection)
                .Build());

        public void ReportError(IError error)
        {
            _resolverTask.ReportError(
                _executionContext.ErrorHandler.Handle(error));
        }

        public IReadOnlyCollection<FieldSelection> CollectFields(
            ObjectType typeContext) =>
            _executionContext.FieldHelper.CollectFields(
                typeContext, FieldSelection.SelectionSet);

        public IReadOnlyCollection<FieldSelection> CollectFields(
            ObjectType typeContext, SelectionSetNode selectionSet) =>
            _executionContext.FieldHelper.CollectFields(
                typeContext, FieldSelection.SelectionSet);
    }
}
