namespace HotChocolate.Types.Filters.Conventions
{
    public interface IFilterConventionDescriptor : IFluent
    {
        IFilterConventionDescriptor ArgumentName(NameString argumentName);

        IFilterConventionDescriptor ElementName(
            NameString name);

        IFilterConventionDescriptor FilterTypeName(
            GetFilterTypeName factory);

        IFilterConventionTypeDescriptor Type(FilterKind kind);

        IFilterConventionDescriptor Ignore(FilterKind kind, bool ignore = true);

        IFilterConventionDefaultOperationDescriptor Operation(FilterOperationKind kind);
    }
}
