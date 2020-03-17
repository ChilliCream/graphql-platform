using HotChocolate.Language;
using HotChocolate.Types;

namespace StrawberryShake.CodeGeneration.Analyzers.Models
{
    public sealed class ArgumentModel
    {
        public ArgumentModel(
            string name,
            IInputType type,
            VariableDefinitionNode variable,
            IValueNode? defaultValue)
        {
            Name = name;
            Type = type;
            Variable = variable;
            DefaultValue = defaultValue;
        }

        public string Name { get; }

        public IInputType Type { get; }

        public VariableDefinitionNode Variable { get; }

        public IValueNode? DefaultValue { get; }
    }
}
