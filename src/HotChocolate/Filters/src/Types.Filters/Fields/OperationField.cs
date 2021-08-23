using System;

namespace HotChocolate.Types.Filters
{
    [Obsolete("Use HotChocolate.Data.")]
    public sealed class FilterOperationField
        : InputField
        , IFilterOperationField
    {
        internal FilterOperationField(FilterOperationDefintion definition)
            : base(definition, default)
        {
            Operation = definition.Operation ??
                throw new ArgumentNullException(nameof(definition.Operation));
        }

        public FilterOperation Operation { get; }
    }
}
