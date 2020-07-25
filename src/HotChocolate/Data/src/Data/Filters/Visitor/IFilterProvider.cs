using System.Diagnostics.CodeAnalysis;
using HotChocolate.Configuration;

namespace HotChocolate.Data.Filters
{
    public interface IFilterProvider
    {
        bool TryGetHandler(
            ITypeDiscoveryContext context,
            FilterInputTypeDefinition typeDefinition,
            FilterFieldDefinition fieldDefinition,
            [NotNullWhen(true)] out FilterFieldHandler? handler);
    }
}

