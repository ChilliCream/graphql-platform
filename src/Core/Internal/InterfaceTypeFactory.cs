using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate
{
    internal sealed class InterfaceTypeFactory
        : ObjectTypeFactoryBase
        , ITypeFactory<InterfaceTypeDefinitionNode, InterfaceType>
    {
        public InterfaceType Create(
            SchemaContext context,
            InterfaceTypeDefinitionNode interfaceTypeDefinition)
        {
            return new InterfaceType(new InterfaceTypeConfig
            {
                SyntaxNode = interfaceTypeDefinition,
                Name = interfaceTypeDefinition.Name.Value,
                Description = interfaceTypeDefinition.Description?.Value,
                Fields = GetFields(context,
                    interfaceTypeDefinition.Name.Value,
                    interfaceTypeDefinition.Fields),
            });
        }
    }
}
