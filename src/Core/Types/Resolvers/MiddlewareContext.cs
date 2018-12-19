using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Resolvers
{
    internal sealed class MiddlewareContext
        : IMiddlewareContext
    {
        private readonly IResolverContext _resolverContext;
        private readonly Func<Task<object>> _resolver;
        private object _result;
        private object _resolvedResult;
        private bool _isResultResolved;

        public MiddlewareContext(
            IResolverContext resolverContext,
            Func<Task<object>> resolver)
        {
            _resolverContext = resolverContext
                ?? throw new ArgumentNullException(nameof(resolverContext));
            _resolver = resolver
                ?? throw new ArgumentNullException(nameof(resolver));
        }

        public object Result
        {
            get
            {
                return _result;
            }
            set
            {
                IsResultModified = true;

                // TODO : rework after error types have been relocated.
                /*
                if (_result is IResolverResult r)
                {
                    if (r.IsError)
                    {
                        _result = QueryError.CreateFieldError(
                            r.ErrorMessage,
                            Path,
                            FieldSelection);
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
                */
                _result = value;
            }
        }

        public bool IsResultModified { get; private set; }

        public ISchema Schema => _resolverContext.Schema;

        public ObjectType ObjectType => _resolverContext.ObjectType;

        public ObjectField Field => _resolverContext.Field;

        public DocumentNode QueryDocument => _resolverContext.QueryDocument;

        public OperationDefinitionNode Operation => _resolverContext.Operation;

        public FieldNode FieldSelection => _resolverContext.FieldSelection;

        public IImmutableStack<object> Source => _resolverContext.Source;

        public Path Path => _resolverContext.Path;

        public CancellationToken CancellationToken => RequestAborted;

        public CancellationToken RequestAborted =>
            _resolverContext.RequestAborted;

        public T Argument<T>(NameString name) =>
            _resolverContext.Argument<T>(name);

        public T CustomContext<T>() =>
            _resolverContext.CustomContext<T>();

        public T CustomProperty<T>(string key) =>
            _resolverContext.CustomProperty<T>(key);

        public T DataLoader<T>(string key) =>
            _resolverContext.DataLoader<T>(key);

        public T Parent<T>() => _resolverContext.Parent<T>();

        public void ReportError(string errorMessage) =>
            _resolverContext.ReportError(errorMessage);

        public void ReportError(IError error) =>
            _resolverContext.ReportError(error);


        public T Resolver<T>() => _resolverContext.Resolver<T>();

        public T Service<T>() => _resolverContext.Service<T>();

        public object Service(Type service) =>
            _resolverContext.Service(service);

        public async Task<T> ResolveAsync<T>()
        {
            if (!_isResultResolved)
            {
                _resolvedResult = await _resolver();
                _isResultResolved = true;
            }

            return (T)_resolvedResult;
        }
    }
}
