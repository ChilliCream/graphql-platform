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
            if (variable == null)
            {
                throw new ArgumentNullException(nameof(variable));
            }

            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            Location = location;
            Variable = variable;
            Type = type;
            DefaultValue = defaultValue;
        }

        public NodeKind Kind { get; } = NodeKind.VariableDefinition;
        public Location Location { get; }
        public VariableNode Variable { get; }
        public ITypeNode Type { get; }
        public IValueNode DefaultValue { get; }
    }
}