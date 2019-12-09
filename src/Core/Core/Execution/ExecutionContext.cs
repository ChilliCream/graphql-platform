using System;
using System.Collections.Generic;
using System.Threading;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Utilities;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution
{
    internal class ExecutionContext
        : IExecutionContext
    {
        private readonly object _syncRoot = new object();
        private readonly IRequestContext _requestContext;
        private readonly FieldCollector _fieldCollector;
        private readonly ICachedQuery _cachedQuery;

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
