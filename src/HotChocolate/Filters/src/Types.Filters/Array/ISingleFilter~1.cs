using System;

namespace HotChocolate.Types.Filters
{
    [Obsolete("Use HotChocolate.Data.")]
    public interface ISingleFilter<out T> : ISingleFilter
    {
        T Element { get; }
    }
}
