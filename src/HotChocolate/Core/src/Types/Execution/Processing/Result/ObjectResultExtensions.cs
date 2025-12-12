using System.Runtime.CompilerServices;
using HotChocolate.Types;

namespace HotChocolate.Execution.Processing;

internal static class ObjectResultExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void InitValueUnsafe(this ObjectResult result, int index, ISelection selection)
        => result.SetValueUnsafe(index, selection.ResponseName, null, selection.Type.Kind is not TypeKind.NonNull);
}
