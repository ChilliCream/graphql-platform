using HotChocolate.Language;

namespace HotChocolate.Types
{
    public interface IInputType
        : ISerializableType
        , IHasRuntimeType
    {
        bool IsInstanceOfType(IValueNode literal);

        bool IsInstanceOfType(object value);

        object ParseLiteral(IValueNode literal);

        IValueNode ParseValue(object value);
    }
}
