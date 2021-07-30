using System;

namespace HotChocolate.Execution.Processing
{
    internal static partial class ValueCompletion
    {
        public static void ReportError(
            IOperationContext operationContext,
            MiddlewareContext resolverContext,
            ISelection selection,
            IError error)
        {
            if (error is AggregateError aggregateError)
            {
                foreach (var innerError in aggregateError.Errors)
                {
                    ReportSingle(innerError);
                }
            }
            else
            {
                ReportSingle(error);
            }

            void ReportSingle(IError singleError)
            {
                AddProcessedError(operationContext.ErrorHandler.Handle(singleError));
            }

            void AddProcessedError(IError processed)
            {
                if (processed is AggregateError ar)
                {
                    foreach (var ie in ar.Errors)
                    {
                        operationContext.Result.AddError(ie, selection.SyntaxNode);
                        operationContext.DiagnosticEvents.ResolverError(resolverContext, ie);
                    }
                }
                else
                {
                    operationContext.Result.AddError(processed, selection.SyntaxNode);
                    operationContext.DiagnosticEvents.ResolverError(resolverContext, processed);
                }
            }
        }

        public static void ReportError(
            IOperationContext operationContext,
            MiddlewareContext resolverContext,
            ISelection selection,
            Path path,
            Exception exception)
        {
            if (exception is null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            if (exception is GraphQLException graphQLException)
            {
                foreach (IError error in graphQLException.Errors)
                {
                    ReportError(operationContext, resolverContext, selection, error);
                }
            }
            else
            {
                IError error = operationContext.ErrorHandler
                    .CreateUnexpectedError(exception)
                    .SetPath(path)
                    .AddLocation(selection.SyntaxNode)
                    .Build();

                ReportError(operationContext, resolverContext, selection, error);
            }
        }
    }
}
