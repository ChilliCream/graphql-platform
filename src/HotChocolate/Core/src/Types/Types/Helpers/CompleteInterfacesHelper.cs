using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors;
using static HotChocolate.Utilities.ErrorHelper;

#nullable enable

namespace HotChocolate.Types.Helpers;

internal static class CompleteInterfacesHelper
{
    public static InterfaceType[] CompleteInterfaces<TInterfaceOrObject>(
        ITypeCompletionContext context,
        IReadOnlyList<TypeReference> interfaceReferences,
        TInterfaceOrObject interfaceOrObject)
        where TInterfaceOrObject : ITypeSystemObject

    {
        if (interfaceReferences.Count == 0)
        {
            return [];
        }

        var implements = new InterfaceType[interfaceReferences.Count];
        var index = 0;

        foreach (var interfaceRef in interfaceReferences)
        {
            if (!context.TryGetType(interfaceRef, out InterfaceType? type))
            {
                context.ReportError(
                    CompleteInterfacesHelper_UnableToResolveInterface(
                        interfaceOrObject));
            }

            if (index == 0 || Array.IndexOf(implements, type, 0, index) == -1)
            {
                implements[index++] = type!;
            }
        }

        if (index < implements.Length)
        {
            Array.Resize(ref implements, index);
        }

        return implements;
    }
}
