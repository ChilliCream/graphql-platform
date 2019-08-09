using System.Globalization;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Utilities;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution
{
    internal partial class ResolverContext
        : IMiddlewareContext
    {
        private IExecutionContext _executionContext;
        private object _result;
        private object _cachedResolverResult;
        private bool _hasCachedResolverResult;
        private IDictionary<string, object> _serializedResult;
        private FieldSelection _fieldSelection;
        private IReadOnlyDictionary<NameString, ArgumentValue> _arguments;

        public ITypeConversion Converter =>
            _executionContext.Converter;

        public ISchema Schema => _executionContext.Schema;

        public DocumentNode Document => _executionContext.Operation.Document;

        public OperationDefinitionNode Operation =>
            _executionContext.Operation.Definition;

        public IDictionary<string, object> ContextData =>
            _executionContext.ContextData;

        public CancellationToken RequestAborted =>
            _executionContext.RequestAborted;

        public bool IsRoot { get; private set; }

        public ObjectType ObjectType => _fieldSelection.Field.DeclaringType;

        public ObjectField Field => _fieldSelection.Field;

        public FieldNode FieldSelection => _fieldSelection.Selection;

        public NameString ResponseName => _fieldSelection.ResponseName;

        public IImmutableStack<object> Source { get; private set; }

        // TODO : is this the right name?
        public object SourceObject { get; private set; }

        public Path Path { get; private set; }

        public IImmutableDictionary<string, object> ScopedContextData
        {
            get;
            set;
        }

        public FieldDelegate Middleware => _fieldSelection.Middleware;

        public Task Task { get; set; }

        public object Result
        {
            get => _result;
            set
            {
                if (value is IResolverResult r)
                {
                    if (r.IsError)
                    {
                        _result = ErrorBuilder.New()
                            .SetMessage(r.ErrorMessage)
                            .SetPath(Path)
                            .AddLocation(FieldSelection)
                            .Build();
                    }
                    else
                    {
                        _result = r.Value;
                    }
                }
                else
                {
                    _result = value;
                }
                IsResultModified = true;
            }
        }

        public bool IsResultModified { get; private set; }

        public Action PropagateNonNullViolation { get; private set; }

        public IVariableValueCollection Variables => _executionContext.Variables;

        public T Parent<T>()
        {
            if (SourceObject is null)
            {
                return default;
            }

            if (SourceObject is T parent)
            {
                return parent;
            }

            if (_executionContext.Converter
                .TryConvert<object, T>(SourceObject, out parent))
            {
                return parent;
            }

            throw new InvalidCastException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    CoreResources.ResolverContext_Parent_InvalidCast,
                    typeof(T).FullName));
        }

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

        public void ReportError(string errorMessage)
        {
            if (string.IsNullOrEmpty(errorMessage))
            {
                throw new ArgumentException(
                    "errorMessage mustn't be null or empty.",
                    nameof(errorMessage));
            }

            ReportError(ErrorBuilder.New()
                .SetMessage(errorMessage)
                .SetPath(Path)
                .AddLocation(FieldSelection)
                .Build());
        }

        public void ReportError(IError error)
        {
            if (error == null)
            {
                throw new ArgumentNullException(nameof(error));
            }

            _executionContext.AddError(
                _executionContext.ErrorHandler.Handle(error));
        }

        public T Service<T>()
        {
            if (typeof(T) == typeof(IServiceProvider))
            {
                return (T)_executionContext.Services;
            }
            return (T)_executionContext.Services.GetRequiredService(typeof(T));
        }

        public object Service(Type service)
        {
            if (service is null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            if (service == typeof(IServiceProvider))
            {
                return _executionContext.Services;
            }
            return _executionContext.Services.GetRequiredService(service);
        }

        public T Resolver<T>() =>
            _executionContext.Activator.GetOrCreateResolver<T>();

        public async Task<T> ResolveAsync<T>()
        {
            if (!_hasCachedResolverResult)
            {
                if (Field.Resolver == null)
                {
                    _cachedResolverResult = default(T);
                }
                else
                {
                    _cachedResolverResult =
                        await Field.Resolver.Invoke(this).ConfigureAwait(false);
                }
                _hasCachedResolverResult = true;
            }

            return (T)_cachedResolverResult;
        }

        public IReadOnlyCollection<FieldSelection> CollectFields(
            ObjectType typeContext) =>
            _executionContext.CollectFields(
                typeContext, FieldSelection.SelectionSet, Path);

        public IReadOnlyCollection<FieldSelection> CollectFields(
            ObjectType typeContext, SelectionSetNode selectionSet) =>
            _executionContext.CollectFields(
                typeContext, FieldSelection.SelectionSet, Path);

        IReadOnlyCollection<IFieldSelection> IResolverContext.CollectFields(
            ObjectType typeContext) => CollectFields(typeContext);

        IReadOnlyCollection<IFieldSelection> IResolverContext.CollectFields(
            ObjectType typeContext, SelectionSetNode selectionSet) =>
            CollectFields(typeContext, selectionSet);
    }
}
