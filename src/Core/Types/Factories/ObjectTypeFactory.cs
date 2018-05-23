using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Types.Factories
{
    internal sealed class ObjectTypeFactory
        : ObjectTypeFactoryBase
        , ITypeFactory<ObjectTypeDefinitionNode, ObjectType>
    {
        public ObjectType Create(
            SchemaContext context,
            ObjectTypeDefinitionNode objectTypeDefinition)
        {
            return new ObjectType(new ObjectTypeConfig
            {
                SyntaxNode = objectTypeDefinition,
                Name = objectTypeDefinition.Name.Value,
                Description = objectTypeDefinition.Description?.Value,
                Fields = GetFields(context,
                    objectTypeDefinition.Name.Value,
                    objectTypeDefinition.Fields),
                Interfaces = () => GetInterfaces(context,
                    objectTypeDefinition.Interfaces)
            });
        }

        private IEnumerable<InterfaceType> GetInterfaces(
            SchemaContext context,
            IReadOnlyCollection<NamedTypeNode> interfaceReferences)
        {
            int i = 0;
            InterfaceType[] interfaces =
                new InterfaceType[interfaceReferences.Count];

            foreach (NamedTypeNode type in interfaceReferences)
            {
                interfaces[i++] = context
                    .GetOutputType<InterfaceType>(type.Name.Value);
            }

            return interfaces;
        }
    }
}
