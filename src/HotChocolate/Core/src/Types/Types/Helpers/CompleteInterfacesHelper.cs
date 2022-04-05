using System;
using System.Collections.Generic;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using static HotChocolate.Utilities.ErrorHelper;

#nullable enable

namespace HotChocolate.Types.Helpers;

internal static class CompleteInterfacesHelper
{
    public static InterfaceType[] CompleteInterfaces<TInterfaceOrObject>(
        ITypeCompletionContext context,
        IReadOnlyList<ITypeReference> interfaceReferences,
        TInterfaceOrObject interfaceOrObject)
        where TInterfaceOrObject : ITypeSystemObject, IHasSyntaxNode

    {
        if (interfaceReferences.Count == 0)
        {
            return Array.Empty<InterfaceType>();
        }

        var implements = new InterfaceType[interfaceReferences.Count];
        var index = 0;

        foreach (ITypeReference interfaceRef in interfaceReferences)
        {
            if (!context.TryGetType(interfaceRef, out InterfaceType? type))
            {
                context.ReportError(
                    CompleteInterfacesHelper_UnableToResolveInterface(
                        interfaceOrObject,
                        interfaceOrObject.SyntaxNode));
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
