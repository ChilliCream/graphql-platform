namespace HotChocolate.Types.Filters.Conventions
{
    public interface IFilterExpressionVisitorDescriptor : IFilterVisitorDescriptor
    {
        IFilterConventionDescriptor And();

        IFilterExpressionTypeDescriptor Type(FilterKind kind);
    }
}
