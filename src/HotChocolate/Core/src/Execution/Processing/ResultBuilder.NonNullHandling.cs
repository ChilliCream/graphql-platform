using System;
using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Execution.Processing;

internal sealed partial class ResultBuilder
{
   private static bool ApplyNonNullViolations(
        List<IError> errors,
        List<NonNullViolation> violations,
        HashSet<FieldNode> errorFields)
    {
        if (violations.Count > 0)
        {
            while (violations.TryPop(out var violation))
            {
                var path = violation.Path;
                ResultData? parent = violation.Parent;

                if (!errorFields.Contains(violation.Selection))
                {
                    errors.Add(ErrorHelper.NonNullOutputFieldViolation(path, violation.Selection));
                }

                while (parent is not null)
                {
                    switch (parent)
                    {
                        case ObjectResult obj:
                            if (obj.Parent is null)
                            {
                                return false;
                            }

                            parent = obj.Parent;
                            break;

                        case ObjectFieldResult field:
                            if (field.IsNullable)
                            {
                                field.Set(field.Name, null, true);
                                parent = null;
                            }
                            else
                            {
                                path = path.Parent;
                                parent = field.Parent;
                            }
                            break;

                        case ListResult list:
                            if (list.IsNullable)
                            {
                                list.SetUnsafe(((IndexerPathSegment)path).Index, null);
                                parent = null;
                                break;
                            }

                            path = path.Parent;
                            parent = parent.Parent;
                            break;

                        default:
                            throw new ArgumentOutOfRangeException(nameof(parent));
                    }
                }
            }
        }

        return true;
    }

   private sealed class NonNullViolation
   {
       public NonNullViolation(FieldNode selection, Path path, ObjectResult parent)
       {
           Selection = selection;
           Path = path;
           Parent = parent;
       }

       public FieldNode Selection { get; }
       public Path Path { get; }
       public ObjectResult Parent { get; }
   }
}
