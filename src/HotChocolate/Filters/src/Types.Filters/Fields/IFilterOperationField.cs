using System;

namespace HotChocolate.Types.Filters
{
    [Obsolete("Use HotChocolate.Data.")]
    public interface IFilterOperationField
        : IInputField
    {
        FilterOperation Operation { get; }
    }
}
