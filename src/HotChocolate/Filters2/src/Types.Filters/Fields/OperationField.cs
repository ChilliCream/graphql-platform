using System;

namespace HotChocolate.Types.Filters
{
    public sealed class FilterOperationField
        : InputField
        , IFilterOperationField
    {
        internal FilterOperationField(FilterOperationDefintion definition)
            : base(definition)
        {
            Operation = definition.Operation ??
                throw new ArgumentNullException(nameof(definition.Operation));
        }

        public FilterOperation Operation { get; }
    }
}
