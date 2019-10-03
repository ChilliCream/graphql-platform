using System;
using System.Collections.Generic;
using System.Text;

namespace HotChocolate.Types.Filters
{
    public interface ISingleFilter<T> : ISingleFilter
    {
        T El { get; }
    }
}
