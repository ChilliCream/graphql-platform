using HotChocolate.Language;

namespace HotChocolate.Types.Filters
{
    public interface IBooleanFilterOperationDescriptor
        : IDescriptor<FilterOperationDefintion>
        , IFluent
    {
        IBooleanFilterFieldDescriptor And();

        IBooleanFilterOperationDescriptor Name(NameString value);

        IBooleanFilterOperationDescriptor Description(string value);

        IBooleanFilterOperationDescriptor Directive<T>(T directiveInstance)
            where T : class;

        IBooleanFilterOperationDescriptor Directive<T>()
            where T : class, new();

        IBooleanFilterOperationDescriptor Directive(
            NameString name,
            params ArgumentNode[] arguments);
    }
}
