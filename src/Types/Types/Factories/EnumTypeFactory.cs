using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Types.Factories
{
    internal sealed class EnumTypeFactory
        : ITypeFactory<EnumTypeDefinitionNode, EnumType>
    {
        public EnumType Create(EnumTypeDefinitionNode node)
        {
            return new EnumType(d =>
            {
                d.SyntaxNode(node)
                    .Name(node.Name.Value)
                    .Description(node.Description?.Value);


                DeclareValues(d, node.Values);
            });
        }

        private void DeclareValues(
            IEnumTypeDescriptor typeDescriptor,
            IReadOnlyCollection<EnumValueDefinitionNode> values)
        {
            foreach (EnumValueDefinitionNode value in values)
            {
                IEnumValueDescriptor valueDescriptor =
                    typeDescriptor.Item(value.Name.Value);

                string deprecactionReason = value.DeprecationReason();
                if (!string.IsNullOrEmpty(deprecactionReason))
                {
                    valueDescriptor.DeprecationReason(deprecactionReason);
                }
            }
        }
    }
}
