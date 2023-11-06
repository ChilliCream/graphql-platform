using System.Collections.Generic;
using HotChocolate.Language;
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

        while (violations.TryPop(out var violation))
        {
            if (fieldErrors.Contains(violation.Selection))
            {
                continue;
            }
            
            var error = NonNullOutputFieldViolation(violation.Path, violation.Selection.SyntaxNode);
            error = _context.ErrorHandler.Handle(error);
            _diagnosticEvents.ResolverError(_context, violation.Selection, error);
            errors.Add(error);
        }
    }

    private sealed class NonNullViolation
    {
        public NonNullViolation(ISelection selection, Path path)
        {
            Selection = selection;
            Path = path;
        }

        public ISelection Selection { get; }

        public Path Path { get; }
    }
}
