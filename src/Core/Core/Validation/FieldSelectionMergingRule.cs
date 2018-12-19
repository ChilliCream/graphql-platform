namespace HotChocolate.Validation
{
    /// <summary>
    /// If multiple field selections with the same response names are
    /// encountered during execution, the field and arguments to execute and
    /// the resulting value should be unambiguous.
    ///
    /// Therefore any two field selections which might both be encountered
    /// for the same object are only valid if they are equivalent.
    ///
    /// During execution, the simultaneous execution of fields with the same
    /// response name is accomplished by MergeSelectionSets() and
    /// CollectFields().
    ///
    /// http://facebook.github.io/graphql/June2018/#sec-Field-Selection-Merging
    /// </summary>
    internal sealed class FieldSelectionMergingRule
        : QueryVisitorValidationErrorBase
    {
        protected override QueryVisitorErrorBase CreateVisitor(ISchema schema)
        {
            return new FieldSelectionMergingVisitor(schema);
        }
    }
}
