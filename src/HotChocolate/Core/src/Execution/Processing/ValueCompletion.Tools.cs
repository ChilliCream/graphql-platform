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
            error = operationContext.ErrorHandler.Handle(error);
            operationContext.Result.AddError(error, selection.SyntaxNode);
            operationContext.DiagnosticEvents.ResolverError(resolverContext, error);
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
