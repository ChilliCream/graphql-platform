using HotChocolate.Configuration;

namespace HotChocolate.Data.Sorting
{
    public interface ISortFieldHandler
    {
        bool CanHandle(
            ITypeDiscoveryContext context,
            SortInputTypeDefinition typeDefinition,
            SortFieldDefinition fieldDefinition);
    }
}
