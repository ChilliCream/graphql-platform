using System;
using System.Collections.Generic;
using HotChocolate.Language;
using static HotChocolate.Execution.ErrorHelper;

namespace HotChocolate.Execution.Processing;

internal sealed partial class ResultBuilder
{
    private bool ApplyNonNullViolations(
        List<IError> errors,
        List<NonNullViolation> violations,
        HashSet<ISelection> fieldErrors)
    {
        if (violations.Count is 0)
        {
            return true;
        }

        while (violations.TryPop(out var violation))
        {
            var path = violation.Path;
            ResultData? parent = violation.Parent;

            if (!fieldErrors.Contains(violation.Selection))
            {
                var error = NonNullOutputFieldViolation(path, violation.Selection.SyntaxNode);
                error = _errorHandler.Handle(error);
                _diagnosticEvents.ResolverError(_operation, violation.Selection, error);
                errors.Add(error);
            }

            while (parent is not null)
            {
                switch (parent)
                {
                    case ObjectResult obj:
                        if (path is not NamePathSegment nps)
                        {
                            return false;
                        }

                        var field = obj.TryGetValue(nps.Name, out _);

                        if (field is null)
                        {
                            return false;
                        }

                        if (field.IsNullable)
                        {
                            field.Set(field.Name, null, true);
                            return true;
                        }

                        _removedResults.Add(obj.PatchId);
                        path = path.Parent;
                        parent = obj.Parent;
                        break;

                    case ListResult list:
                        if (list.IsNullable)
                        {
                            list.SetUnsafe(((IndexerPathSegment)path).Index, null);
                            return true;
                        }

                        _removedResults.Add(list.PatchId);
                        path = path.Parent;
                        parent = parent.Parent;
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(parent));
                }
            }
        }

        return false;
    }

    private sealed class NonNullViolation
    {
        public NonNullViolation(ISelection selection, Path path, ObjectResult parent)
        {
            Selection = selection;
            Path = path;
            Parent = parent;
        }

        public ISelection Selection { get; }

        public Path Path { get; }

        public ObjectResult Parent { get; }
    }
}
