using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
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
            DirectiveLookup directives,
            IDictionary<string, object> contextData,
            CancellationToken requestAborted)
        {
            Schema = schema
                ?? throw new ArgumentNullException(nameof(schema));
            ServiceScope = serviceScope
                ?? throw new ArgumentNullException(nameof(serviceScope));
            Operation = operation
                ?? throw new ArgumentNullException(nameof(operation));
            Variables = variables
                ?? throw new ArgumentNullException(nameof(variables));
            Directives = directives
                ?? throw new ArgumentNullException(nameof(directives));
            ContextData = contextData
                ?? throw new ArgumentNullException(nameof(contextData));
            RequestAborted = requestAborted;

            ErrorHandler = serviceScope.ServiceProvider
                .GetRequiredService<IErrorHandler>();

            Result = new QueryResult();

            FieldHelper = CreateFieldHelper(
                variables,
                new FragmentCollection(schema, operation.Query),
                directives,
                Result.Errors);

            Activator = new Activator(serviceScope.ServiceProvider);
        }

        public ISchema Schema { get; }

        public IRequestServiceScope ServiceScope { get; }

        public IServiceProvider Services => ServiceScope.ServiceProvider;

        public IErrorHandler ErrorHandler { get; }

        public IOperation Operation { get; }

        public IVariableCollection Variables { get; }

        public DirectiveLookup Directives { get; }

        public IQueryResult Result { get; private set; }

        public IDictionary<string, object> ContextData { get; private set; }

        public CancellationToken RequestAborted { get; }

        public IFieldHelper FieldHelper { get; }

        public IActivator Activator { get; }

        private static IFieldHelper CreateFieldHelper(
            IVariableCollection variables,
            FragmentCollection fragments,
            DirectiveLookup directives,
            ICollection<IError> errors)
        {
            var fieldCollector = new FieldCollector(
                variables, fragments);

            return new FieldHelper(
                fieldCollector, directives,
                variables, errors);
        }

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
            var cloned = (ExecutionContext)base.MemberwiseClone();
            cloned.ContextData = new ConcurrentDictionary<string, object>(
                cloned.ContextData);
            cloned.Result = new QueryResult();
            return cloned;
        }
    }
}
