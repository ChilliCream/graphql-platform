using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using HotChocolate.Configuration;
using HotChocolate.Resolvers;

namespace HotChocolate.Data.Filters
{
    public interface IFilterProvider
    {
        bool TryGetHandler(
            ITypeDiscoveryContext context,
            FilterInputTypeDefinition typeDefinition,
            FilterFieldDefinition fieldDefinition,
            [NotNullWhen(true)] out FilterFieldHandler? handler);

        Task ExecuteAsync<TEntityType>(
            FieldDelegate next,
            IMiddlewareContext context);
    }
}

