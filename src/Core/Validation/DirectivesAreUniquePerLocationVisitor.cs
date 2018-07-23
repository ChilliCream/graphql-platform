namespace HotChocolate.Validation
{
    internal sealed class DirectivesAreUniquePerLocationVisitor
        : QueryVisitor
    {
        public DirectivesAreUniquePerLocationVisitor(ISchema schema)
            : base(schema)
        {
        }
    }

}
