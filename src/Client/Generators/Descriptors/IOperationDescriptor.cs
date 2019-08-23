using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Types;

namespace StrawberryShake.Generators.Descriptors
{
    public interface IOperationDescriptor
        : ICodeDescriptor
    {
        OperationDefinitionNode Operation { get; }

        IReadOnlyList<IArgumentDescriptor> Arguments { get; }
    }

    public interface IArgumentDescriptor
    {
        string Name { get; }

        IType Type { get; }

        VariableDefinitionNode VariableDefinition { get; }
    }

    public interface IInputClassDescriptor
        : ICodeDescriptor
    {
        InputObjectType Type { get; }

        IReadOnlyList<IInputFieldDescriptor> Arguments { get; }
    }

    public interface IInputFieldDescriptor
    {
        string Name { get; }

        IInputField Field { get; }

        IType Type { get; }
    }
}
