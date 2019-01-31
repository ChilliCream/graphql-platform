using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    internal sealed class ResolverTask
    {
        private readonly IExecutionContext _executionContext;
        private readonly IDictionary<string, object> _result;

        public ResolverTask(
            IExecutionContext executionContext,
            ObjectType objectType,
            FieldSelection fieldSelection,
            Path path,
            IImmutableStack<object> source,
            IDictionary<string, object> result,
            IImmutableDictionary<string, object> scopedContextData)
        {
            _executionContext = executionContext;
            Source = source;
            ObjectType = objectType;
            FieldSelection = fieldSelection;
            FieldType = fieldSelection.Field.Type;
            Path = path;
            _result = result;
            ScopedContextData = scopedContextData;

            ResolverContext = new ResolverContext(
                executionContext, this,
                executionContext.RequestAborted);

            FieldDelegate = executionContext.FieldHelper
                .CreateMiddleware(fieldSelection);
        }

        public IImmutableStack<object> Source { get; }

        public ObjectType ObjectType { get; }

        public FieldSelection FieldSelection { get; }

        public IType FieldType { get; }

        public Path Path { get; }

        public IResolverContext ResolverContext { get; }

        public Task<object> Task { get; set; }

        public object ResolverResult { get; set; }

        public FieldDelegate FieldDelegate { get; }
        
        public IImmutableDictionary<string, object> ScopedContextData
        {
            get;
            set;
        }

        public QueryExecutionDiagnostics Diagnostics
        {
            get
            {
                return _executionContext.Diagnostics;
            }
        }

        public void IntegrateResult(object value)
        {
            _result[FieldSelection.ResponseName] = value;
        }

        public void ReportError(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                // TODO : Resources
                throw new ArgumentException(
                    "The error message cannot be null or empty.",
                    nameof(message));
            }

            ReportError(CreateError(message));
        }

        public void ReportError(IError error)
        {
            if (error == null)
            {
                throw new ArgumentNullException(nameof(error));
            }

            _executionContext.AddError(error);
        }

        public IError CreateError(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                throw new ArgumentException(
                    "The error message cannot be null or empty.",
                    nameof(message));
            }

            return QueryError.CreateFieldError(
                message,
                Path,
                FieldSelection.Selection);
        }
    }
}
