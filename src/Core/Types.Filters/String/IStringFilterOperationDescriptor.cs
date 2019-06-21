using HotChocolate.Language;

namespace HotChocolate.Types.Filters
{
    public interface IStringFilterOperationDescriptor
        : IDescriptor<FilterOperationDefintion>
        , IFluent
    {
        IStringFilterFieldDescriptor And();

        IStringFilterOperationDescriptor Name(NameString value);

        IStringFilterOperationDescriptor Description(string value);

        IStringFilterOperationDescriptor Directive<T>(T directiveInstance)
            where T : class;

        IStringFilterOperationDescriptor Directive<T>()
            where T : class, new();

        IStringFilterOperationDescriptor Directive(
            NameString name,
            params ArgumentNode[] arguments);
    }
}
