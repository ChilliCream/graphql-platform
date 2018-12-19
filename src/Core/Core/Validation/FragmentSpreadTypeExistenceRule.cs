namespace HotChocolate.Validation
{
    /// <summary>
    /// Fragments must be specified on types that exist in the schema.
    /// This applies for both named and inline fragments.
    /// If they are not defined in the schema, the query does not validate.
    ///
    /// http://facebook.github.io/graphql/June2018/#sec-Fragment-Spread-Type-Existence
    /// </summary>
    internal sealed class FragmentSpreadTypeExistenceRule
        : QueryVisitorValidationErrorBase
    {
        protected override QueryVisitorErrorBase CreateVisitor(ISchema schema)
        {
            return new FragmentSpreadTypeExistenceVisitor(schema);
        }
    }
}
