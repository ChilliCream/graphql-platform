using HotChocolate.Language;

namespace HotChocolate.Types
{
    public interface IArgumentDescriptor
    {
        IArgumentDescriptor Description(string description);
        IArgumentDescriptor Type<TInputType>()
            where TInputType : IInputType;
        IArgumentDescriptor DefaultValue(IValueNode defaultValue);
        IArgumentDescriptor DefaultValue(object defaultValue);
    }
}
