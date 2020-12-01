using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Types;

namespace StrawberryShake.Generators.Descriptors
{
    public class ArgumentDescriptor
        : IArgumentDescriptor
    {
        public ArgumentDescriptor(
            string name,
            IType type,
            VariableDefinitionNode variableDefinition,
            IInputClassDescriptor? inputObjectType)
        {
            Name = name
                ?? throw new ArgumentNullException(nameof(name));
            Type = type
                ?? throw new ArgumentNullException(nameof(type));
            VariableDefinition = variableDefinition
                ?? throw new ArgumentNullException(nameof(variableDefinition));
            InputObjectType = inputObjectType;
        }

        public string Name { get; }

        public IType Type { get; }

        public IInputClassDescriptor? InputObjectType { get; }

        public VariableDefinitionNode VariableDefinition { get; }

        public IEnumerable<ICodeDescriptor> GetChildren()
        {
            if (InputObjectType is { })
            {
                yield return InputObjectType;
            }
        }
    }
}
