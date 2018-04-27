using HotChocolate.Language;

namespace HotChocolate
{
    public interface IInputTypeParser
    {
        object Parse(IValueNode literal);
    }
}
