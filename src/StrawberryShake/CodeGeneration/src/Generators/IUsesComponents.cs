using System.Collections.Generic;

namespace StrawberryShake.Generators
{
    public interface IUsesComponents
    {
        IReadOnlyList<string> Components { get; }
    }
}
