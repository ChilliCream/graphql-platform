using System;
using System.Buffers;
using System.Collections.Generic;
using System.Threading;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Utilities;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution
{
    internal class ExecutionContext
        : IExecutionContext
    {
        private static readonly ArrayPool<IResolverContext> _contextPool =
            ArrayPool<IResolverContext>.Create(1024 * 1000, 256);

        private readonly object _syncRoot = new object();
        private readonly IRequestContext _requestContext;
        private readonly FieldCollector _fieldCollector;
        private readonly ICachedQuery _cachedQuery;
        private IResolverContext[] _trackedContextBuffer;
        private int _buffered = 0;

        public ExecutionContext(
            ISchema schema,
            IOperation operation,
            IRequestContext requestContext,
            CancellationToken requestAborted)
        {
            Schema = schema ??
                throw new ArgumentNullException(nameof(schema));
            Operation = operation ??
                throw new ArgumentNullException(nameof(operation));
            _requestContext = requestContext
                ?? throw new ArgumentNullException(nameof(requestContext));

            RequestAborted = requestAborted;

            _cachedQuery = _requestContext.CachedQuery;

            ErrorHandler = requestContext.ServiceScope.ServiceProvider
                .GetRequiredService<IErrorHandler>();

            Result = new QueryResult();

            var fragments = new FragmentCollection(
                schema, operation.Document);

            Converter = _requestContext.ServiceScope
                .ServiceProvider.GetTypeConversion();

            _fieldCollector = new FieldCollector(
                fragments, requestContext.ResolveMiddleware, Converter);

            Activator = new Activator(
                requestContext.ServiceScope.ServiceProvider);
        }

        internal ExecutionContext(ISchema schema, IErrorHandler errorHandler, IOperation operation, IQueryResult result, CancellationToken requestAborted, IActivator activator, ITypeConversion converter)
        {
            this.Schema = schema;
            this.ErrorHandler = errorHandler;
            this.Operation = operation;
            this.Result = result;
            this.RequestAborted = requestAborted;
            this.Activator = activator;
            this.Converter = converter;

        }
        public ISchema Schema { get; }

        public IRequestServiceScope ServiceScope =>
            _requestContext.ServiceScope;

        public IServiceProvider Services =>
            ServiceScope.ServiceProvider;

        public IErrorHandler ErrorHandler { get; }

        public IOperation Operation { get; }

        public IVariableValueCollection Variables => Operation.Variables;

        public IQueryResult Result { get; private set; }

        public IDictionary<string, object> ContextData =>
            _requestContext.ContextData;

        public CancellationToken RequestAborted { get; }

        public IActivator Activator { get; }

        public QueryExecutionDiagnostics Diagnostics =>
            _requestContext.Diagnostics;

        public ITypeConversion Converter { get; }

        public void AddError(IError error)
        {
            if (error == null)
            {
                throw new ArgumentNullException(nameof(error));
            }

            lock (_syncRoot)
            {
                Result.Errors.Add(error);
            }
        }

        public IReadOnlyList<FieldSelection> CollectFields(
            ObjectType objectType,
            SelectionSetNode selectionSet,
            Path path)
        {
            if (objectType == null)
            {
                throw new ArgumentNullException(nameof(objectType));
            }

            if (selectionSet == null)
            {
                throw new ArgumentNullException(nameof(selectionSet));
            }

            IReadOnlyList<FieldSelection> fields =
                _cachedQuery.GetOrCollectFields(
                    objectType,
                    selectionSet,
                    () => _fieldCollector.CollectFields(
                        objectType, selectionSet, path));

            var visibleFields = new List<FieldSelection>();

            for (int i = 0; i < fields.Count; i++)
            {
                if (fields[i].IsVisible(Variables))
                {
                    visibleFields.Add(fields[i]);
                }
            }

            return visibleFields;
        }

        public void TrackContext(IResolverContext resolverContext)
        {

            if (_trackedContextBuffer is null)
            {
                _trackedContextBuffer = _contextPool.Rent(256);
            }

            if (_trackedContextBuffer.Length <= _buffered)
            {
                IResolverContext[] next = _contextPool.Rent(
                    _trackedContextBuffer.Length * 2);
                _trackedContextBuffer.AsSpan().CopyTo(next);
                _contextPool.Return(_trackedContextBuffer);
                _trackedContextBuffer = next;
            }

            _trackedContextBuffer[_buffered++] = resolverContext;

        }

        public ReadOnlySpan<IResolverContext> GetTrackedContexts()
        {
            return _trackedContextBuffer.AsSpan().Slice(0, _buffered);
        }

        public IExecutionContext Clone()
        {
            return new ExecutionContext
            (
                Schema,
                Operation,
                _requestContext.Clone(),
                RequestAborted
            );
        }
    }
}
