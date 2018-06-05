using System.Collections.Generic;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Types.Factories
{
    internal sealed class ObjectTypeFactory
        : ObjectTypeFactoryBase
        , ITypeFactory<ObjectTypeDefinitionNode, ObjectType>
    {
        public ObjectType Create(
            ObjectTypeDefinitionNode objectTypeDefinition)
        {
            return new ObjectType(new ObjectTypeConfig
            {
                SyntaxNode = objectTypeDefinition,
                Name = objectTypeDefinition.Name.Value,
                Description = objectTypeDefinition.Description?.Value,
                Fields = GetFields(
                    objectTypeDefinition.Name.Value,
                    objectTypeDefinition.Fields),
                Interfaces = t => GetInterfaces(t,
                    objectTypeDefinition.Interfaces)
            });
        }

        private IEnumerable<InterfaceType> GetInterfaces(
            ITypeRegistry typeRegistry,
            IReadOnlyCollection<NamedTypeNode> interfaceReferences)
        {
            int i = 0;
            InterfaceType[] interfaces =
                new InterfaceType[interfaceReferences.Count];

            foreach (NamedTypeNode typeNode in interfaceReferences)
            {
                interfaces[i++] = typeRegistry.GetType<InterfaceType>(
                    typeNode.Name.Value);
            }

            return interfaces;
        }
    }
}
