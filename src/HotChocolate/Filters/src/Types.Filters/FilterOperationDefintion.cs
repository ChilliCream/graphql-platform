using System;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Filters
{
    /// <summary>
    /// Represents a specific filter that can be applied on a field.
    /// </summary>
    [Obsolete("Use HotChocolate.Data.")]
    public class FilterOperationDefintion
        : InputFieldDefinition
    {
        /// <summary>
        /// Gets or sets the operation description for this field.
        /// </summary>
        public FilterOperation? Operation { get; set; }
    }
}
