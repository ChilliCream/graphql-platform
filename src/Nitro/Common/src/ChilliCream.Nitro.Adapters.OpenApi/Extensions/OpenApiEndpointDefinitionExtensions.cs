using ChilliCream.Nitro.Adapters.OpenApi.Serialization;
using HotChocolate.Adapters.OpenApi;
using HotChocolate.Language;

namespace ChilliCream.Nitro.Adapters.OpenApi.Extensions;

public static class OpenApiEndpointDefinitionExtensions
{
    extension(OpenApiEndpointDefinition openApiEndpointDefinition)
    {
        public static OpenApiEndpointDefinition Create(
            OpenApiEndpointSettings settings,
            string httpMethod,
            string route,
            DocumentNode document)
        {
            var operationDefinition = document.Definitions.OfType<OperationDefinitionNode>().SingleOrDefault() ??
                throw new ArgumentException("The document must contain exactly one operation definition.",
                    nameof(document));

            var fragmentReferences = FragmentReferenceFinder.Find(document);

            return new OpenApiEndpointDefinition(
                httpMethod,
                route,
                settings.Description,
                settings.RouteParameters,
                settings.QueryParameters,
                settings.BodyVariableName,
                document,
                operationDefinition,
                fragmentReferences.Local,
                fragmentReferences.External);
        }

        public OpenApiEndpointSettings ToSettings()
            => new(
                openApiEndpointDefinition.Description,
                openApiEndpointDefinition.RouteParameters,
                openApiEndpointDefinition.QueryParameters,
                openApiEndpointDefinition.BodyVariableName);
    }
}
