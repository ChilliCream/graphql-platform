using System;
using System.Collections.Generic;

namespace HotChocolate.Language
{
    /// <summary>
    /// A GraphQL Input Object defines a set of input fields; the input fields are either
    /// scalars, enums, or other input objects. This allows arguments to accept arbitrarily
    /// complex structs.
    /// https://graphql.github.io/graphql-spec/June2018/#sec-Input-Objects
    /// </summary>
    public sealed class InputValueDefinitionNode
        : NamedSyntaxNode
    {
        public InputValueDefinitionNode(
            Location? location,
            NameNode name,
            StringValueNode? description,
            ITypeNode type,
            IValueNode? defaultValue,
            IReadOnlyList<DirectiveNode> directives)
            : base(location, name, directives)
        {
            Description = description;
            Type = type ?? throw new ArgumentNullException(nameof(type));
            DefaultValue = defaultValue;
        }

        public override NodeKind Kind { get; } = NodeKind.InputValueDefinition;

        public StringValueNode? Description { get; }

        public ITypeNode Type { get; }

        public IValueNode? DefaultValue { get; }

        public InputValueDefinitionNode WithLocation(Location? location)
        {
            return new InputValueDefinitionNode(
                location, Name, Description,
                Type, DefaultValue, Directives);
        }

        public InputValueDefinitionNode WithName(NameNode name)
        {
            return new InputValueDefinitionNode(
                Location, name, Description,
                Type, DefaultValue, Directives);
        }

        public InputValueDefinitionNode WithDescription(
            StringValueNode? description)
        {
            return new InputValueDefinitionNode(
                Location, Name, description,
                Type, DefaultValue, Directives);
        }

        public InputValueDefinitionNode WithType(ITypeNode type)
        {
            return new InputValueDefinitionNode(
                Location, Name, Description,
                type, DefaultValue, Directives);
        }

        public InputValueDefinitionNode WithDefaultValue(
            IValueNode defaultValue)
        {
            return new InputValueDefinitionNode(
                Location, Name, Description,
                Type, defaultValue, Directives);
        }

        public InputValueDefinitionNode WithDirectives(
            IReadOnlyList<DirectiveNode> directives)
        {
            return new InputValueDefinitionNode(
                Location, Name, Description,
                Type, DefaultValue, directives);
        }
    }
}
