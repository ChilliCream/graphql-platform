using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Configuration;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    internal sealed class ResolverTask
    {
        private readonly IExecutionContext _executionContext;

        public ResolverTask(
            IExecutionContext executionContext,
            ObjectType objectType,
            FieldSelection fieldSelection,
            Path path,
            ImmutableStack<object> source,
            OrderedDictionary result)
        {
            _executionContext = executionContext;

            Source = source;
            ObjectType = objectType;
            FieldSelection = fieldSelection;
            FieldType = fieldSelection.Field.Type;
            Path = path;
            Result = result;

            ResolverContext = new ResolverContext(
                executionContext, this,
                executionContext.CancellationToken);

            Options = executionContext.Options;

            ExecuteMiddleware = executionContext.GetMiddleware(
                objectType, fieldSelection.Selection);
            HasMiddleware = ExecuteMiddleware != null;
        }

        public IReadOnlySchemaOptions Options { get; }

        public ImmutableStack<object> Source { get; }

        public ObjectType ObjectType { get; }

        public FieldSelection FieldSelection { get; }

        public IType FieldType { get; }

        public Path Path { get; }

        private OrderedDictionary Result { get; }

        public IResolverContext ResolverContext { get; }

        public Task<object> Task { get; set; }

        public object ResolverResult { get; set; }

        public ExecuteMiddleware ExecuteMiddleware { get; }

        public bool HasMiddleware { get; }

        public void IntegrateResult(object value)
        {
            Result[FieldSelection.ResponseName] = value;
        }

        public void ReportError(string message)
        {
            ReportError(CreateError(message));
        }

        public void ReportError(IQueryError error)
        {
            _executionContext.ReportError(error);
        }

        public IQueryError CreateError(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                throw new ArgumentException(
                    "A field error mustn't be null or empty.",
                    nameof(message));
            }

            return QueryError.CreateFieldError(
                message,
                Path,
                FieldSelection.Selection);
        }
    }
}
