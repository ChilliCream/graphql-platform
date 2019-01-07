using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using HotChocolate.Configuration;
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
            IDictionary<string, object> result)
        {
            _executionContext = executionContext;
            Source = source;
            ObjectType = objectType;
            FieldSelection = fieldSelection;
            FieldType = fieldSelection.Field.Type;
            Path = path;
            _result = result;

            ResolverContext = new ResolverContext(
                executionContext, this,
                executionContext.RequestAborted);

            ExecuteMiddleware = executionContext.FieldHelper.CreateMiddleware(
                objectType, fieldSelection.Selection);
            HasMiddleware = ExecuteMiddleware != null;
        }

        public IImmutableStack<object> Source { get; }

        public ObjectType ObjectType { get; }

        public FieldSelection FieldSelection { get; }

        public IType FieldType { get; }

        public Path Path { get; }

        public IResolverContext ResolverContext { get; }

        public Task<object> Task { get; set; }

        public object ResolverResult { get; set; }

        public ExecuteMiddleware ExecuteMiddleware { get; }

        public bool HasMiddleware { get; }

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
