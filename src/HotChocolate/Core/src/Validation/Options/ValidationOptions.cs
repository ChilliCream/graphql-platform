using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Validation.Options
{
    public class ValidationOptions : IMaxExecutionDepthOptionsAccessor
    {
        public IList<IDocumentValidatorRule> Rules { get; } =
            new List<IDocumentValidatorRule>();

        /// <summary>
        /// Gets the maximum allowed depth of a query. The default value is
        /// <see langword="null"/>. The minimum allowed value is <c>1</c>.
        /// </summary>
        public int? MaxAllowedExecutionDepth { get; set; }
    }
}
