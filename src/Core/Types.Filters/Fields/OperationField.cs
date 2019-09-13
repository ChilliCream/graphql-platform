using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Filters
{
    internal sealed class FilterOperationField
        : InputField
    {
        public FilterOperationField(FilterOperationDefintion definition)
            : base(definition)
        {
            Operation = definition.Operation;
        }

        public FilterOperation Operation { get; }
    }
}
