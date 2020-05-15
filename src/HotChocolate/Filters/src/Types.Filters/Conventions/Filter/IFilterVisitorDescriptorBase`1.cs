namespace HotChocolate.Types.Filters.Conventions
{
    public interface IFilterVisitorDescriptorBase<out T>
        : IFilterVisitorDescriptor
        where T : FilterVisitorDefinitionBase
    {
        T CreateDefinition();
    }
}
