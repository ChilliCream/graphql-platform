using System;
using System.Collections.Generic;

namespace HotChocolate.Language
{
    public sealed class FieldDefinitionNode
        : NamedSyntaxNode
    {
        public FieldDefinitionNode(
            Location? location,
            NameNode name,
            StringValueNode? description,
            IReadOnlyList<InputValueDefinitionNode> arguments,
            ITypeNode type,
            IReadOnlyList<DirectiveNode> directives)
            : base(location, name, directives)
        {
            Description = description;
            Arguments = arguments
                ?? throw new ArgumentNullException(nameof(arguments));
            Type = type
                ?? throw new ArgumentNullException(nameof(type));
        }

        public override NodeKind Kind { get; } = NodeKind.FieldDefinition;

        public StringValueNode? Description { get; }

        public IReadOnlyList<InputValueDefinitionNode> Arguments { get; }

        public ITypeNode Type { get; }

        public FieldDefinitionNode WithLocation(Location? location)
        {
            return new FieldDefinitionNode(
                location, Name, Description,
                Arguments, Type, Directives);
        }

        public FieldDefinitionNode WithName(NameNode name)
        {
            return new FieldDefinitionNode(
                Location, name, Description,
                Arguments, Type, Directives);
        }

        public FieldDefinitionNode WithDescription(
            StringValueNode? description)
        {
            return new FieldDefinitionNode(
                Location, Name, description,
                Arguments, Type, Directives);
        }

        public FieldDefinitionNode WithArguments(
            IReadOnlyList<InputValueDefinitionNode> arguments)
        {
            return new FieldDefinitionNode(
                Location, Name, Description,
                arguments, Type, Directives);
        }

        public FieldDefinitionNode WithType(ITypeNode type)
        {
            return new FieldDefinitionNode(
                Location, Name, Description,
                Arguments, type, Directives);
        }

        public FieldDefinitionNode WithDirectives(
            IReadOnlyList<DirectiveNode> directives)
        {
            return new FieldDefinitionNode(
                Location, Name, Description,
                Arguments, Type, directives);
        }
    }
}
