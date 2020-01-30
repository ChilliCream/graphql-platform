namespace HotChocolate.Validation
{
    /// <summary>
    /// Fragments are declared on a type and will only apply when the
    /// runtime object type matches the type condition.
    ///
    /// They also are spread within the context of a parent type.
    ///
    /// A fragment spread is only valid if its type condition could ever
    /// apply within the parent type.
    ///
    /// http://facebook.github.io/graphql/June2018/#sec-Fragment-spread-is-possible
    /// </summary>
    internal sealed class FragmentSpreadIsPossibleRule
        : QueryVisitorValidationErrorBase
    {
        protected override QueryVisitorErrorBase CreateVisitor(ISchema schema)
        {
            return new FragmentSpreadIsPossibleVisitor(schema);
        }
    }
}
