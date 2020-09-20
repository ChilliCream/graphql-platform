using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Filters
{
    /// <summary>
    /// Represents a field that can be filtered.
    /// </summary>
    public class FilterFieldDefintion
        : InputFieldDefinition
    {
        /// <summary>
        /// Gets the filters that can be applied on this field.
        /// </summary>
        public IBindableList<FilterOperationDefintion> Filters { get; } =
            new BindableList<FilterOperationDefintion>();
    }
}
