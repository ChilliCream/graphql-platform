using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    internal sealed class DirectiveContext
        : IDirectiveContext
    {
        private readonly IResolverContext _resolverContext;
        private readonly Func<Task<object>> _resolver;
        private object _result;
        private object _resolvedResult;
        private bool _isResultResolved;

        public DirectiveContext(
            IResolverContext resolverContext,
            Func<Task<object>> resolver)
        {
            _resolverContext = resolverContext
                ?? throw new ArgumentNullException(nameof(resolverContext));
            _resolver = resolver
                ?? throw new ArgumentNullException(nameof(resolver));
        }

        public IDirective Directive { get; set; }

        public object Result
        {
            get
            {
                return _result;
            }
            set
            {
                IsResultModified = true;
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

        public ImmutableStack<object> Source => _resolverContext.Source;

        public Path Path => _resolverContext.Path;

        public CancellationToken CancellationToken => RequestAborted;

        public CancellationToken RequestAborted =>
            _resolverContext.RequestAborted;

        public T Argument<T>(string name) =>
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

        public T Resolver<T>() => _resolverContext.Resolver<T>();

        public T Service<T>() => _resolverContext.Service<T>();

        public async Task<T> ResolveAsync<T>()
        {
            if (!_isResultResolved)
            {
                _resolvedResult = await _resolver();
                _isResultResolved = true;
            }

            if (_resolvedResult is IQueryError error)
            {
                throw new QueryException(error);
            }
            else if (_resolvedResult is IEnumerable<IQueryError> errors)
            {
                throw new QueryException(errors);
            }
            else
            {
                return (T)_resolvedResult;
            }
        }
    }
}
