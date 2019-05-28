using System;

namespace HotChocolate.Utilities
{
    public interface ITypeConversion
    {
        bool TryConvert(Type from, Type to,
            object source, out object converted);

        object Convert(Type from, Type to, object source);
    }
}
