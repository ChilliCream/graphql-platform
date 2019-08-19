using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Sorting
{
    public class SortFieldDefintion
        : InputFieldDefinition
    {
        public IBindableList<SortOperationDefintion> Sorts { get; } =
            new BindableList<SortOperationDefintion>();
    }
}
