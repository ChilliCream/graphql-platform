using System.Collections.Generic;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Utilities;

#nullable enable

namespace HotChocolate.Types.Interceptors
{
    internal sealed class MiddlewareValidationTypeInterceptor : TypeInterceptor
    {
        public override void OnValidateType(
            ITypeSystemObjectContext validationContext,
            DefinitionBase? definition,
            IDictionary<string, object?> contextData)
        {
            if (definition is ObjectTypeDefinition objectTypeDef)
            {
                foreach (ObjectFieldDefinition field in objectTypeDef.Fields)
                {
                    if (field.MiddlewareDefinitions.Count > 1)
                    {
                        ValidatePipeline(
                            validationContext.Type,
                            new FieldCoordinate(validationContext.Type.Name, field.Name),
                            field.SyntaxNode,
                            field.MiddlewareDefinitions);
                    }
                }
            }
        }

        private void ValidatePipeline(
            ITypeSystemObject type,
            FieldCoordinate field,
            ISyntaxNode? syntaxNode,
            IList<FieldMiddlewareDefinition> middlewareDefinitions)
        {
            var usePaging = false;
            var useProjections = false;
            var useFiltering = false;
            var useSorting = false;
            var error = false;

            foreach (FieldMiddlewareDefinition definition in middlewareDefinitions)
            {
                if (definition.Key is not null)
                {
                    switch (definition.Key)
                    {
                        case WellKnownMiddleware.DbContext:
                            if (usePaging || useProjections || useFiltering || useSorting)
                            {
                                error = true;
                            }
                            break;

                        case WellKnownMiddleware.Paging:
                            if (useProjections || useFiltering || useSorting)
                            {
                                error = true;
                                break;
                            }
                            usePaging = true;
                            break;

                        case WellKnownMiddleware.Projection:
                            if (useFiltering || useSorting)
                            {
                                error = true;
                                break;
                            }
                            useProjections = true;
                            break;

                        case WellKnownMiddleware.Filtering:
                            useFiltering = true;
                            break;

                        case WellKnownMiddleware.Sorting:
                            useSorting = true;
                            break;
                    }
                }
            }

            if (error)
            {
                throw new SchemaException(
                    ErrorHelper.MiddlewareOrderInvalid(field, type, syntaxNode));
            }
        }
    }
}
