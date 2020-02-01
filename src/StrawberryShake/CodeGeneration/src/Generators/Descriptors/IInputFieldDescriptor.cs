using HotChocolate.Types;

namespace StrawberryShake.Generators.Descriptors
{
    public interface IInputFieldDescriptor
        : ICodeDescriptor
    {
        IInputField Field { get; }

        IType Type { get; }

        IInputClassDescriptor? InputObjectType { get; }
    }
}
