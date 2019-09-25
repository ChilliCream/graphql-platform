using System.Collections.Generic;

namespace StrawberryShake
{
    public interface IInputObject
    {
        IReadOnlyDictionary<string, object> ToDictionary();
    }
}
