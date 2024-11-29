using System.Text;
using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Utilities;

#nullable enable

namespace HotChocolate.Types.Interceptors;

internal sealed class MiddlewareValidationTypeInterceptor : TypeInterceptor
{
    private const string _useDbContext = "UseDbContext";
    private const string _usePaging = "UsePaging";
    private const string _useProjection = "UseProjection";
    private const string _useFiltering = "UseFiltering";
    private const string _useSorting = "UseSorting";

    private readonly HashSet<string> _names = [];

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
                        new SchemaCoordinate(validationContext.Type.Name, field.Name),
                        field.MiddlewareDefinitions);
                }
            }
        }
    }

    private void ValidatePipeline(
        ITypeSystemObject type,
        SchemaCoordinate fieldCoordinate,
        IList<FieldMiddlewareDefinition> middlewareDefinitions)
    {
        _names.Clear();

        var usePaging = false;
        var useProjections = false;
        var useFiltering = false;
        var useSorting = false;
        var error = false;
        HashSet<string>? duplicates = null;

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

                        if (!_names.Add(definition.Key))
                        {
                            (duplicates ??= []).Add(_useDbContext);
                        }
                        break;

                    case WellKnownMiddleware.Paging:
                        if (useProjections || useFiltering || useSorting)
                        {
                            error = true;
                            break;
                        }

                        if (!_names.Add(definition.Key))
                        {
                            (duplicates ??= []).Add(_usePaging);
                        }

                        usePaging = true;
                        break;

                    case WellKnownMiddleware.Projection:
                        if (useFiltering || useSorting)
                        {
                            error = true;
                            break;
                        }

                        if (!_names.Add(definition.Key))
                        {
                            (duplicates ??= []).Add(_useProjection);
                        }

                        useProjections = true;
                        break;

                    case WellKnownMiddleware.Filtering:
                        if (!_names.Add(definition.Key))
                        {
                            (duplicates ??= []).Add(_useFiltering);
                        }
                        useFiltering = true;
                        break;

                    case WellKnownMiddleware.Sorting:
                        if (!_names.Add(definition.Key))
                        {
                            (duplicates ??= []).Add(_useSorting);
                        }
                        useSorting = true;
                        break;
                }
            }
        }

        if (duplicates?.Count > 0)
        {
            throw new SchemaException(
                ErrorHelper.DuplicateDataMiddlewareDetected(
                    fieldCoordinate,
                    type,
                    duplicates));
        }

        if (error)
        {
            throw new SchemaException(
                ErrorHelper.MiddlewareOrderInvalid(
                    fieldCoordinate,
                    type,
                    PrintPipeline(middlewareDefinitions)));
        }
    }

    private static string PrintPipeline(
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
                        sb.Append(_useDbContext);
                        break;

                    case WellKnownMiddleware.Paging:
                        other = false;
                        PrintNext();
                        sb.Append(_usePaging);
                        break;

                    case WellKnownMiddleware.Projection:
                        other = false;
                        PrintNext();
                        sb.Append(_useProjection);
                        break;

                    case WellKnownMiddleware.Filtering:
                        other = false;
                        PrintNext();
                        sb.Append(_useFiltering);
                        break;

                    case WellKnownMiddleware.Sorting:
                        other = false;
                        PrintNext();
                        sb.Append(_useSorting);
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
