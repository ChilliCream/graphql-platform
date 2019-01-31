using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using HotChocolate.Execution.Instrumentation;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution
{
    internal class ExecutionContext
        : IExecutionContext
    {
        private readonly object _syncRoot = new object();
        private readonly IRequestContext _requestContext;

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

            ErrorHandler = requestContext.ServiceScope.ServiceProvider
                .GetRequiredService<IErrorHandler>();

            Result = new QueryResult();

            var fragments = new FragmentCollection(
                schema, operation.Document);

            var fieldCollector = new FieldCollector(
                operation.Variables, fragments);

            FieldHelper = new FieldHelper(
                fieldCollector, requestContext.ResolveMiddleware,
                AddError);

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

        public IVariableCollection Variables => Operation.Variables;

        public IQueryResult Result { get; private set; }

        public IDictionary<string, object> ContextData =>
            _requestContext.ContextData;

        public CancellationToken RequestAborted { get; }

        public IFieldHelper FieldHelper { get; }

        public IActivator Activator { get; }

        public QueryExecutionDiagnostics Diagnostics =>
            _requestContext.Diagnostics;

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
