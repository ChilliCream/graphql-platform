using ChilliCream.Nitro.Adapters.OpenApi.Serialization;
using HotChocolate.Adapters.OpenApi;
using HotChocolate.Language;

namespace ChilliCream.Nitro.Adapters.OpenApi.Extensions;

public static class OpenApiModelDefinitionExtensions
{
    extension(OpenApiModelDefinition openApiModelDefinition)
    {
        public static OpenApiModelDefinition Create(
            OpenApiModelSettings settings,
            string name,
            DocumentNode document)
        {
            if (!document.Definitions.All(d => d is FragmentDefinitionNode))
            {
                throw new ArgumentException("The document can only contain fragment definitions.",
                    nameof(document));
            }

            var fragmentDefinition = document.Definitions.OfType<FragmentDefinitionNode>().FirstOrDefault() ??
                throw new ArgumentException("The document must contain at least one fragment definition.",
                    nameof(document));

            if (fragmentDefinition.Name.Value != name)
            {
                throw new ArgumentException("The provided name does not match the name of the first fragment definition.",
                    nameof(name));
            }

            var fragmentReferences = FragmentReferenceFinder.Find(document, fragmentDefinition);

            return new OpenApiModelDefinition(
                name,
                settings.Description,
                document,
                fragmentDefinition,
                fragmentReferences.Local,
                fragmentReferences.External);
        }
    }
}
