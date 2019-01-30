using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Resolvers;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution
{
    internal class ExecutionContext
        : IExecutionContext
    {
        private readonly object _syncRoot = new object();

        public ExecutionContext(
            ISchema schema,
            IRequestServiceScope serviceScope,
            IOperation operation,
            IVariableCollection variables,
            Func<FieldSelection, FieldDelegate> middlewareResolver,
            IDictionary<string, object> contextData,
            CancellationToken requestAborted,
            QueryExecutionDiagnostics diagnostics)
        {
            if (middlewareResolver == null)
            {
                throw new ArgumentNullException(nameof(middlewareResolver));
            }

            Schema = schema ??
                throw new ArgumentNullException(nameof(schema));
            ServiceScope = serviceScope ??
                throw new ArgumentNullException(nameof(serviceScope));
            Operation = operation ??
                throw new ArgumentNullException(nameof(operation));
            Variables = variables ??
                throw new ArgumentNullException(nameof(variables));
            ContextData = contextData ??
                throw new ArgumentNullException(nameof(contextData));
            RequestAborted = requestAborted;
            Diagnostics = diagnostics ??
                throw new ArgumentNullException(nameof(diagnostics));

            ErrorHandler = serviceScope.ServiceProvider
                .GetRequiredService<IErrorHandler>();

            Result = new QueryResult();

            var fragments = new FragmentCollection(
                schema, operation.Document);

            var fieldCollector = new FieldCollector(
                variables, fragments);

            FieldHelper = new FieldHelper(
                fieldCollector, middlewareResolver, AddError);

            Activator = new Activator(serviceScope.ServiceProvider);
        }

        public ISchema Schema { get; }

        public IRequestServiceScope ServiceScope { get; }

        public IServiceProvider Services => ServiceScope.ServiceProvider;

        public IErrorHandler ErrorHandler { get; }

        public IOperation Operation { get; }

        public IVariableCollection Variables { get; }

        public IQueryResult Result { get; private set; }

        public IDictionary<string, object> ContextData { get; private set; }

        public CancellationToken RequestAborted { get; }

        public IFieldHelper FieldHelper { get; }

        public IActivator Activator { get; }

        public QueryExecutionDiagnostics Diagnostics { get; }

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
            var cloned = (ExecutionContext)MemberwiseClone();

            cloned.ContextData = new ConcurrentDictionary<string, object>(
                cloned.ContextData);
            cloned.Result = new QueryResult();

            return cloned;
        }
    }
}
