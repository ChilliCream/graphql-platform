using HotChocolate.Configuration;

namespace HotChocolate.Data.Filters
{
    public interface IFilterFieldHandler
    {
        bool CanHandle(
            ITypeDiscoveryContext context,
            IFilterInputTypeDefinition typeDefinition,
            IFilterFieldDefinition fieldDefinition);
    }
}
