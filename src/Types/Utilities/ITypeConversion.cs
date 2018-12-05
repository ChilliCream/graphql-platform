using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Linq;

namespace HotChocolate.Utilities
{
    public interface ITypeConversion
    {
        bool TryConvert(Type from, Type to,
            object source, out object converted);
    }
}
