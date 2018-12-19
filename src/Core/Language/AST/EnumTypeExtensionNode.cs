using System.Collections.Generic;

namespace HotChocolate.Language
{
    public sealed class EnumTypeExtensionNode
        : EnumTypeDefinitionNodeBase
        , ITypeExtensionNode
    {
        public EnumTypeExtensionNode(
            Location location,
            NameNode name,
            IReadOnlyCollection<DirectiveNode> directives,
            IReadOnlyCollection<EnumValueDefinitionNode> values)
            : base(location, name, directives, values)
        {
        }

        public override NodeKind Kind { get; } = NodeKind.EnumTypeExtension;

        public EnumTypeExtensionNode WithLocation(Location location)
        {
            return new EnumTypeExtensionNode(
                location, Name,
                Directives, Values);
        }

        public EnumTypeExtensionNode WithName(NameNode name)
        {
            return new EnumTypeExtensionNode(
                Location, name,
                Directives, Values);
        }

        public EnumTypeExtensionNode WithDirectives(
            IReadOnlyCollection<DirectiveNode> directives)
        {
            return new EnumTypeExtensionNode(
                Location, Name,
                directives, Values);
        }

        public EnumTypeExtensionNode WithValues(
            IReadOnlyCollection<EnumValueDefinitionNode> values)
        {
            return new EnumTypeExtensionNode(
                Location, Name,
                Directives, values);
        }
    }
}
