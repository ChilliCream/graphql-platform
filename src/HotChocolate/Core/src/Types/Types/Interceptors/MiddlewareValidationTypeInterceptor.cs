using System.Collections.Generic;
using System.Text;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Utilities;

#nullable enable

namespace HotChocolate.Types.Interceptors;

internal sealed class MiddlewareValidationTypeInterceptor : TypeInterceptor
{
    public override void OnValidateType(
        ITypeSystemObjectContext validationContext,
        DefinitionBase definition)
    {
        if (validationContext.DescriptorContext.Options.ValidatePipelineOrder &&
            definition is ObjectTypeDefinition objectTypeDef)
        {
            foreach (var field in objectTypeDef.Fields)
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

        foreach (var definition in middlewareDefinitions)
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
                ErrorHelper.MiddlewareOrderInvalid(
                    field,
                    type,
                    syntaxNode,
                    PrintPipeline(middlewareDefinitions)));
        }
    }

    private string PrintPipeline(
        IList<FieldMiddlewareDefinition> middlewareDefinitions)
    {
        var sb = new StringBuilder();
        var next = false;
        var other = false;

        foreach (var definition in middlewareDefinitions)
        {
            if (definition.Key is not null)
            {
                switch (definition.Key)
                {
                    case WellKnownMiddleware.DbContext:
                        other = false;
                        PrintNext();
                        sb.Append("UseDbContext");
                        break;

                    case WellKnownMiddleware.Paging:
                        other = false;
                        PrintNext();
                        sb.Append("UsePaging");
                        break;

                    case WellKnownMiddleware.Projection:
                        other = false;
                        PrintNext();
                        sb.Append("UseProjection");
                        break;

                    case WellKnownMiddleware.Filtering:
                        other = false;
                        PrintNext();
                        sb.Append("UseFiltering");
                        break;

                    case WellKnownMiddleware.Sorting:
                        other = false;
                        PrintNext();
                        sb.Append("UseSorting");
                        break;

                    default:
                        PrintOther();
                        break;
                }
            }
            else
            {
                PrintOther();
            }

            next = true;
        }

        return sb.ToString();

        void PrintNext()
        {
            if (next)
            {
                sb.Append(" -> ");
            }
        }

        void PrintOther()
        {
            if (!other)
            {
                sb.Append("...");
                other = true;
            }
        }
    }
}
