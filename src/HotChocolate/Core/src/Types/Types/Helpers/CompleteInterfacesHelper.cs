using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using static HotChocolate.Utilities.ErrorHelper;

#nullable enable

namespace HotChocolate.Types
{
    internal static class CompleteInterfacesHelper
    {
        public static void CompleteInterfaces(
            ITypeCompletionContext context,
            IReadOnlyList<ITypeReference> interfaceReferences,
            Type clrType,
            ICollection<InterfaceType> interfaces,
            ITypeSystemObject interfaceOrObject,
            ISyntaxNode? node)
        {
            foreach (ITypeReference interfaceRef in interfaceReferences)
            {
                if (!context.TryGetType(interfaceRef, out InterfaceType type))
                {
                    context.ReportError(
                        CompleteInterfacesHelper_UnableToResolveInterface(
                            interfaceOrObject, node));
                }

                if (!interfaces.Contains(type))
                {
                    interfaces.Add(type);
                }
            }
        }
    }
}
