using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Linq;

namespace HotChocolate.Utilities
{
    // TODO : The name should realy be ITypeConverter .... but what should we call the ITypeConverter?
    public interface ITypeConversion
    {
        bool TryConvert(Type from, Type to,
            object source, out object converted);

        object Convert(Type from, Type to, object source);
    }
}
