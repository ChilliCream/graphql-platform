using System;
using System.Collections.Generic;
using System.Threading;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Utilities;
using HotChocolate.Execution.Utilities;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution
{
    internal class ExecutionContext
        : IExecutionContext
    {
        private readonly object _syncRoot = new object();
        private readonly IRequestContext _requestContext;
        private readonly FieldCollector _fieldCollector;

        public ExecutionContext(
            ISchema schema,
            IPreparedOperation operation,
            IVariableValueCollection variableValues,
            object rootValue,
            IRequestContext requestContext,
            CancellationToken requestAborted)
        {
            Schema = schema
                ?? throw new ArgumentNullException(nameof(schema));
            Operation = operation
                ?? throw new ArgumentNullException(nameof(operation));
            Variables = variableValues
                ?? throw new ArgumentNullException(nameof(variableValues));
            RootValue = rootValue;
            _requestContext = requestContext
                ?? throw new ArgumentNullException(nameof(requestContext));
            RequestAborted = requestAborted;

            Result = new QueryResult();
            Converter = Services.GetTypeConversion();
            ErrorHandler = Services.GetRequiredService<IErrorHandler>();
            Activator = new Activator(Services);
        }


        public ISchema Schema { get; }

        public IRequestServiceScope ServiceScope =>
            _requestContext.ServiceScope;

        public IServiceProvider Services =>
            ServiceScope.ServiceProvider;

        public IErrorHandler ErrorHandler { get; }

        public IPreparedOperation Operation { get; }

        public IVariableValueCollection Variables { get; }

        public IQueryResult Result { get; private set; }

        public IDictionary<string, object> ContextData =>
            _requestContext.ContextData;

        public CancellationToken RequestAborted { get; }

        public IActivator Activator { get; }

        public QueryExecutionDiagnostics Diagnostics =>
            _requestContext.Diagnostics;

        public ITypeConversion Converter { get; }

        public object RootValue { get; }

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

        public IReadOnlyList<IPreparedSelection> CollectFields(
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

            IPreparedSelectionList selections = Operation.GetSelections(selectionSet, objectType);

            if (selections.IsFinal)
            {
                return selections;
            }

            var visibleFields = new List<IPreparedSelection>();

            for (int i = 0; i < selections.Count; i++)
            {
                IPreparedSelection selection = selections[i];
                if (selection.IsFinal || selection.IsVisible(Variables))
                {
                    visibleFields.Add(selection);
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
                Variables,
                RootValue,
                _requestContext.Clone(),
                RequestAborted
            );
        }

        public IReadOnlyList<IPreparedSelection> CollectFields(ObjectType objectType, SelectionSetNode selectionSet)
        {
            throw new NotImplementedException();
        }
    }
}
