using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    public interface IArgumentCoercionHandler
    {
        object CoerceValue(IInputField argument, object value);
    }
}
