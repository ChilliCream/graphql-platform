using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    public interface IArgumentCoercionHandler
    {
        IValueNode PrepareValue(IInputField argument, IValueNode literal);

        object CoerceValue(IInputField argument, object value);
    }
}
