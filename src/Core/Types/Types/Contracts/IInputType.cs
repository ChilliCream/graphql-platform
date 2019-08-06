using HotChocolate.Language;

namespace HotChocolate.Types
{
    public interface IInputType
        : ISerializableType
        , IHasClrType
    {
        bool IsInstanceOfType(IValueNode literal);

        bool IsInstanceOfType(object value);

        object ParseLiteral(IValueNode literal);

        IValueNode ParseValue(object value);
    }
}
