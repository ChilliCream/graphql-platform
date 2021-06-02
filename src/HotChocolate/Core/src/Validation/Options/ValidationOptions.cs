using System.Collections.Generic;

namespace HotChocolate.Validation.Options
{
    /// <summary>
    /// The validation options.
    /// </summary>
    public class ValidationOptions : IMaxExecutionDepthOptionsAccessor
    {
        /// <summary>
        /// Gets the document rules of the validation.
        /// </summary>
        public IList<IDocumentValidatorRule> Rules { get; } =
            new List<IDocumentValidatorRule>();

        /// <summary>
        /// Gets the maximum allowed depth of a query. The default value is
        /// <see langword="null"/>. The minimum allowed value is <c>1</c>.
        /// </summary>
        public int? MaxAllowedExecutionDepth { get; set; }
    }
}
