using HotChocolate.Language;

namespace HotChocolate.Types.Filters
{
    public interface IComparableFilterOperationDescriptor
        : IDescriptor<FilterOperationDefintion>
        , IFluent
    {
        IComparableFilterFieldDescriptor And();

        IComparableFilterOperationDescriptor Name(NameString value);

        IComparableFilterOperationDescriptor Description(string value);

        IComparableFilterOperationDescriptor Directive<T>(T directiveInstance)
            where T : class;

        IComparableFilterOperationDescriptor Directive<T>()
            where T : class, new();

        IComparableFilterOperationDescriptor Directive(
            NameString name,
            params ArgumentNode[] arguments);
    }
}
