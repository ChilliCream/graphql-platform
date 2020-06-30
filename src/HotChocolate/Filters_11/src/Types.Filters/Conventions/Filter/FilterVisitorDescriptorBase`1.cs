namespace HotChocolate.Types.Filters.Conventions
{
    public abstract class FilterVisitorDescriptorBase<T>
        : IFilterVisitorDescriptorBase<T>
        where T : FilterVisitorDefinitionBase
    {
        protected abstract T Definition { get; }

        public abstract T CreateDefinition();
    }
}
