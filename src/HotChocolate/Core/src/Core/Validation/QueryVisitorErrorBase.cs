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

        public ICollection<IError> Errors { get; } =
            new List<IError>();
    }

}
