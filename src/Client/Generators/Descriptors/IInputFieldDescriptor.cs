using HotChocolate.Types;

namespace StrawberryShake.Generators.Descriptors
{
    public interface IInputFieldDescriptor
    {
        string Name { get; }

        IInputField Field { get; }

        IType Type { get; }
    }
}
