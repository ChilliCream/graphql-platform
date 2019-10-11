using System;
using System.Collections.Generic;
using System.Text;

namespace HotChocolate.Types.Filters
{
    public interface ISingleFilter<out T> : ISingleFilter
    {
        T El { get; }
    }
}
