using System.Runtime.CompilerServices;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;
using static HotChocolate.Execution.ErrorHelper;

namespace HotChocolate.Execution.Processing;

internal sealed partial class ResultBuilder
{
    private void ApplyNonNullViolations(
        List<IError> errors,
        List<NonNullViolation> violations,
        HashSet<ISelection> fieldErrors)
    {
        if (violations.Count == 0)
        {
            return;
        }

        var errorHandler = _context.Schema.Services.GetRequiredService<IErrorHandler>();

        while (violations.TryPop(out var violation))
        {
            if (fieldErrors.Contains(violation.Selection))
            {
                continue;
            }

            if (_errorPaths.Contains(violation.Path))
            {
                continue;
            }

            var error = NonNullOutputFieldViolation(violation.Path, violation.Selection.SyntaxNode);
            error =  errorHandler.Handle(error);
            _diagnosticEvents.ExecutionError(_context, ErrorKind.FieldError, [error], null);
            errors.Add(error);
        }
    }

    private sealed class NonNullViolation(ISelection selection, Path path)
    {
        public ISelection Selection { get; } = selection;

        public Path Path { get; } = path;
    }
}
