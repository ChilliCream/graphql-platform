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
        public ExecutionContext(
            ISchema schema,
            IServiceProvider services,
            IOperation operation,
            IVariableCollection variables,
            DirectiveLookup directives,
            CancellationToken requestAborted)
        {
            Schema = schema
                ?? throw new ArgumentNullException(nameof(schema));
            Services = services
                ?? throw new ArgumentNullException(nameof(services));
            Operation = operation
                ?? throw new ArgumentNullException(nameof(operation));
            Variables = variables
                ?? throw new ArgumentNullException(nameof(variables));
            Directives = directives
                ?? throw new ArgumentNullException(nameof(directives));
            RequestAborted = requestAborted;

            ErrorHandler = services.GetRequiredService<IErrorHandler>();

            Response = new QueryResonse();
            ContextData = new ConcurrentDictionary<string, object>();

            FieldHelper = CreateFieldHelper(
                variables,
                new FragmentCollection(schema, operation.Query),
                directives,
                Response.Errors);

            Activator = new Activator(services);
        }

        public ISchema Schema { get; }

        public IServiceProvider Services { get; }

        public IErrorHandler ErrorHandler { get; }

        public IOperation Operation { get; }

        public IVariableCollection Variables { get; }

        public DirectiveLookup Directives { get; }

        public IQueryResponse Response { get; private set; }

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

        public IExecutionContext Clone()
        {
            var cloned = (ExecutionContext)base.MemberwiseClone();
            cloned.ContextData = new ConcurrentDictionary<string, object>(
                cloned.ContextData);
            cloned.Response = new QueryResonse();
            return cloned;
        }
    }
}
