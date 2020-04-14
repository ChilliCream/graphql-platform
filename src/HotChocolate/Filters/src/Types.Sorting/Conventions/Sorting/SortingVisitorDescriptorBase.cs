namespace HotChocolate.Types.Sorting.Conventions
{
    public abstract class SortingVisitorDescriptorBase<T>
        : ISortingVisitorDescriptorBase<T>
        where T : SortingVisitorDefinitionBase
    {
        protected abstract T Definition { get; }

        public abstract T CreateDefinition();
    }
}
