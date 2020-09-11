using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Data.Sorting
{
    public interface ISortOperationHandler
    {
        bool CanHandle(
            ITypeDiscoveryContext context,
            EnumTypeDefinition typeDefinition,
            SortEnumValueDefinition valueDefinition);
    }
}
