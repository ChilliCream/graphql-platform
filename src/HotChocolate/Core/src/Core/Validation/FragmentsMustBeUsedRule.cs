namespace HotChocolate.Validation
{
    /// <summary>
    /// Defined fragments must be used within a document.
    /// 
    /// http://facebook.github.io/graphql/June2018/#sec-Fragments-Must-Be-Used
    /// </summary>
    internal sealed class FragmentsMustBeUsedRule
        : QueryVisitorValidationErrorBase
    {
        protected override QueryVisitorErrorBase CreateVisitor(ISchema schema)
        {
            return new FragmentsMustBeUsedVisitor(schema);
        }
    }
}
