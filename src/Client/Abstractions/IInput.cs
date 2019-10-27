using System.Collections.Generic;

namespace StrawberryShake
{
    public interface IInput
    {
        IReadOnlyList<InputValue> GetChangedProperties();
    }
}
