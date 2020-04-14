namespace HotChocolate.Types.Sorting.Conventions
{
    public interface ISortingVisitorDescriptorBase<out T>
       where T : SortingVisitorDefinitionBase
    {
        T CreateDefinition();
    }
}
