using System;

namespace HotChocolate.Language
{
    public sealed class VariableDefinitionNode
        : ISyntaxNode
    {
        public VariableDefinitionNode(
            Location location,
            VariableNode variable,
            ITypeNode type,
            IValueNode defaultValue)
        {
            Location = location;
            Variable = variable 
                ?? throw new ArgumentNullException(nameof(variable));
            Type = type 
                ?? throw new ArgumentNullException(nameof(type));
            DefaultValue = defaultValue;
        }

        public NodeKind Kind { get; } = NodeKind.VariableDefinition;

        public Location Location { get; }

        public VariableNode Variable { get; }

        public ITypeNode Type { get; }

        public IValueNode DefaultValue { get; }

        public VariableDefinitionNode WithLocation(Location location)
        {
            return new VariableDefinitionNode(
                location, Variable, Type,
                DefaultValue);
        }

        public VariableDefinitionNode WithVariable(VariableNode variable)
        {
            return new VariableDefinitionNode(
                Location, variable, Type,
                DefaultValue);
        }


        public VariableDefinitionNode WithType(ITypeNode type)
        {
            return new VariableDefinitionNode(
                Location, Variable, type,
                DefaultValue);
        }

        public VariableDefinitionNode WithDefaultValue(IValueNode defaultValue)
        {
            return new VariableDefinitionNode(
                Location, Variable, Type,
                defaultValue);
        }
    }
}
