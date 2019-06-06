using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Filters
{
    /// <summary>
    /// Represents a specific filter that can be applied on a field.
    /// </summary>
    public class FilterOperationDefintion
        : InputFieldDefinition
    {
        /// <summary>
        /// Gets or sets the filter operation (e.g. contains, greater than ...).
        /// </summary>
        public NameString Operation { get; set; }
    }
}
