using System;
using HotChocolate.Language;

namespace HotChocolate.Execution
{
    internal static class ExecutionHelper
    {
        public static IQueryError CreateErrorFromException(
            this Schema schema, Exception exception)
        {
            if (schema.Options.DeveloperMode)
            {
                return new QueryError(
                    $"{exception.Message}\r\n\r\n{exception.StackTrace}");
            }
            else
            {
                return new QueryError("Unexpected execution error.");
            }
        }

        public static IQueryError CreateErrorFromException(
            this Schema schema, Exception exception, FieldNode fieldSelection)
        {
            if (schema.Options.DeveloperMode)
            {
                return new FieldError(
                    $"{exception.Message}\r\n\r\n{exception.StackTrace}",
                    fieldSelection);
            }
            else
            {
                return new FieldError(
                    "Unexpected execution error.",
                    fieldSelection);
            }
        }
    }
}
