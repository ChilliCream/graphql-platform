using System;
using System.Collections.Generic;

namespace HotChocolate.Language
{
    public sealed class VariableDefinitionNode
        : ISyntaxNode
        , IHasDirectives
    {
        public VariableDefinitionNode(
            Location? location,
            VariableNode variable,
            ITypeNode type,
            IValueNode? defaultValue,
            IReadOnlyList<DirectiveNode> directives)
        {
            Location = location;
            Variable = variable
                ?? throw new ArgumentNullException(nameof(variable));
            Type = type
                ?? throw new ArgumentNullException(nameof(type));
            DefaultValue = defaultValue;
            Directives = directives
                ?? throw new ArgumentNullException(nameof(directives));
        }

        public NodeKind Kind { get; } = NodeKind.VariableDefinition;

        public Location? Location { get; }

        public VariableNode Variable { get; }

        public ITypeNode Type { get; }

        public IValueNode? DefaultValue { get; }

        public IReadOnlyList<DirectiveNode> Directives { get; }

        public VariableDefinitionNode WithLocation(Location? location)
        {
            return new VariableDefinitionNode(
                location, Variable, Type,
                DefaultValue, Directives);
        }

        public VariableDefinitionNode WithVariable(VariableNode variable)
        {
            return new VariableDefinitionNode(
                Location, variable, Type,
                DefaultValue, Directives);
        }

        public VariableDefinitionNode WithType(ITypeNode type)
        {
            return new VariableDefinitionNode(
                Location, Variable, type,
                DefaultValue, Directives);
        }

        public VariableDefinitionNode WithDefaultValue(IValueNode? defaultValue)
        {
            return new VariableDefinitionNode(
                Location, Variable, Type,
                defaultValue, Directives);
        }

        public VariableDefinitionNode WithDirectives(
            IReadOnlyList<DirectiveNode> directives)
        {
            return new VariableDefinitionNode(
                Location, Variable, Type,
                DefaultValue, directives);
        }
    }
}
