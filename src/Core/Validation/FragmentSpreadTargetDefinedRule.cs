using System.Linq;

namespace HotChocolate.Validation
{
    internal sealed class FragmentSpreadTargetDefinedRule
        : QueryVisitorValidationErrorBase
    {
        protected override QueryVisitorErrorBase CreateVisitor(ISchema schema)
        {
            return new FragmentSpreadTargetDefinedVisitor(schema);
        }
    }
}
