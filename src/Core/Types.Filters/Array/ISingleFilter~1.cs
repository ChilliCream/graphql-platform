using System;
using System.Collections.Generic;
using System.Text;

namespace HotChocolate.Types.Filters.Array
{
    public interface ISingleFilter<T> : ISingleFilter
    {
        T Elm { get; }
    }
}
