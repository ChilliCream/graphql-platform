namespace HotChocolate.Validation
{
    /// <summary>
    /// Every argument provided to a field or directive must be defined 
    /// in the set of possible arguments of that field or directive.
    /// 
    /// http://facebook.github.io/graphql/June2018/#sec-Argument-Names
    /// </summary>
    internal sealed class ArgumentNamesRule
        : QueryVisitorValidationErrorBase
    {
        protected override QueryVisitorErrorBase CreateVisitor(ISchema schema)
        {
            return new ArgumentNamesVisitor(schema);
        }
    }
}
