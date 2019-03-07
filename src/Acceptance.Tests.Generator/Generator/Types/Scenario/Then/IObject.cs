using System.Collections.Generic;

namespace Generator
{
    internal interface IObject
    {
        IEnumerable<string> Properties { get; }
    }
}
