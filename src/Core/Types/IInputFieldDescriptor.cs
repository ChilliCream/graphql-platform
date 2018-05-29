using HotChocolate.Language;

namespace HotChocolate.Types
{
    public interface IInputFieldDescriptor
    {
        IInputFieldDescriptor Description(string description);
        IInputFieldDescriptor Type<TInputType>()
            where TInputType : IInputType;
        IInputFieldDescriptor DefaultValue(IValueNode defaultValue);
        IInputFieldDescriptor DefaultValue(object defaultValue);
    }
}
