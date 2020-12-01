using HotChocolate.Language;
using HotChocolate.Types;

namespace StrawberryShake.Generators.Descriptors
{
    public interface IArgumentDescriptor
        : ICodeDescriptor
    {
        IType Type { get; }

        IInputClassDescriptor? InputObjectType { get; }

        VariableDefinitionNode VariableDefinition { get; }
    }
}
