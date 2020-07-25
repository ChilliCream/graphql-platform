using HotChocolate.Configuration;

namespace HotChocolate.Data.Filters
{

    public class FilterFieldHandler
    {
        public virtual bool CanHandle(
            ITypeDiscoveryContext context,
            FilterInputTypeDefinition typeDefinition,
            FilterFieldDefinition fieldDefinition) => false;
    }
}
