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
        public static void Complete(
            ITypeCompletionContext context,
            IComplexOutputTypeDefinition definition,
            Type clrType,
            ICollection<InterfaceType> interfaces,
            ITypeSystemObject interfaceOrObject,
            ISyntaxNode? node)
        {
            if (clrType != typeof(object))
            {
                TryInferInterfaceUsageFromClrType(context, clrType, interfaces);
            }

            if (definition.KnownClrTypes.Count > 0)
            {
                definition.KnownClrTypes.Remove(typeof(object));

                foreach (Type type in definition.KnownClrTypes.Distinct())
                {
                    TryInferInterfaceUsageFromClrType(context, type, interfaces);
                }
            }

            foreach (ITypeReference interfaceRef in definition.Interfaces)
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

        private static void TryInferInterfaceUsageFromClrType(
            ITypeCompletionContext context,
            Type clrType,
            ICollection<InterfaceType> interfaces)
        {
            foreach (Type interfaceType in clrType.GetInterfaces())
            {
                if (context.TryGetType(
                    context.DescriptorContext.TypeInspector.GetTypeRef(
                        interfaceType, TypeContext.Output),
                    out InterfaceType type) &&
                    !interfaces.Contains(type))
                {
                    interfaces.Add(type);
                }
            }
        }
    }
}
