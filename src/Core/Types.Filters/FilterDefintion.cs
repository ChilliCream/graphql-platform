using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Filters
{
    /// <summary>
    /// Represents a specific filter that can be applied on a field.
    /// </summary>
    public class FilterDefintion
        : InputFieldDefinition
    {
        /// <summary>
        /// Gets or sets the filter kind (e.g. contains, greater than ...).
        /// </summary>
        public NameString Kind { get; set; }
    }
}
