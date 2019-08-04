using HotChocolate.Language;

namespace HotChocolate.Types
{
    public interface IInputType
        : IType
        , IHasClrType
        , ISerializableType
    {
        bool IsInstanceOfType(IValueNode literal);

        bool IsInstanceOfType(object value);

        object ParseLiteral(IValueNode literal);

        IValueNode ParseValue(object value);
    }
}
