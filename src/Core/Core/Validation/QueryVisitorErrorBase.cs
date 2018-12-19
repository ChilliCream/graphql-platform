using System.Collections.Generic;

namespace HotChocolate.Validation
{
    internal class QueryVisitorErrorBase
        : QueryVisitor
    {
        protected QueryVisitorErrorBase(ISchema schema)
            : base(schema)
        {
        }

        public ICollection<ValidationError> Errors { get; } =
            new List<ValidationError>();
    }

}
