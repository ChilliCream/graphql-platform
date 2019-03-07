using System;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Descriptors
{
    internal static class DescriptorHelpers
    {
        public static ITypeReference SetMoreSpecificType<TDescription>(
            this TDescription description,
            Type type,
            TypeContext context)
            where TDescription : FieldDefinitionBase
        {
            throw new NotImplementedException();
        }

        public static ITypeReference SetMoreSpecificType<TDescription>(
            this TDescription description,
            ITypeNode typeNode)
            where TDescription : FieldDefinitionBase
        {
            throw new NotImplementedException();
        }
    }
}
