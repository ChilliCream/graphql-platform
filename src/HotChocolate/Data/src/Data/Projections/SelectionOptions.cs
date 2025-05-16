using HotChocolate.Execution.Processing;
using HotChocolate.Types;

namespace HotChocolate.Data.Projections;

[Flags]
public enum SelectionFlags
{
    None = 0,
    FirstOrDefault = 1,
    SingleOrDefault = 2,
    MemberIsList = 4,
}

public static class SelectionFlagsExtensions
{
    public static bool IsFirstOrDefault(this ISelection selection)
    {
        var flags = selection.Field.Features.Get<SelectionFlags>();
        return (flags & SelectionFlags.FirstOrDefault) == SelectionFlags.FirstOrDefault;
    }

    public static bool IsSingleOrDefault(this ISelection selection)
    {
        var flags = selection.Field.Features.Get<SelectionFlags>();
        return (flags & SelectionFlags.SingleOrDefault) == SelectionFlags.SingleOrDefault;
    }

    public static bool IsMemberIsList(this ISelection selection)
    {
        var flags = selection.Field.Features.Get<SelectionFlags>();
        return (flags & SelectionFlags.MemberIsList) == SelectionFlags.MemberIsList;
    }

    public static bool IsSelectionFlags(this ISelection selection, SelectionFlags flags)
    {
        var actualFlags = selection.Field.Features.Get<SelectionFlags>();
        return (actualFlags & flags) == flags;
    }

    public static bool IsSelectionFlags(this IFieldDefinition field, SelectionFlags flags)
    {
        var actualFlags = field.Features.Get<SelectionFlags>();
        return (actualFlags & flags) == flags;
    }
}
