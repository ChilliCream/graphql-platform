using HotChocolate.Types;

namespace StrawberryShake.Generators.Descriptors
{
    public class InputFieldDescriptor
        : IInputFieldDescriptor
    {
        public InputFieldDescriptor(string name, IInputField field, IType type)
        {
            Name = name;
            Field = field;
            Type = type;
        }

        public string Name { get; }

        public IInputField Field { get; }

        public IType Type { get; }
    }
}
