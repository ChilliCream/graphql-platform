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

            if (_errorPaths.Contains(violation.Path))
            {
                continue;
            }

            var error = NonNullOutputFieldViolation(violation.Path, violation.Selection.SyntaxNode);
            error = _context.ErrorHandler.Handle(error);
            _diagnosticEvents.ResolverError(_context, violation.Selection, error);
            errors.Add(error);
        }
    }

    private sealed class NonNullViolation(ISelection selection, Path path)
    {
        public ISelection Selection { get; } = selection;

        public Path Path { get; } = path;
    }
}
