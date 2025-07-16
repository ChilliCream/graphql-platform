using System.Text;
using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors.Configurations;
using HotChocolate.Utilities;

#nullable enable

namespace HotChocolate.Types.Interceptors;

internal sealed class MiddlewareValidationTypeInterceptor : TypeInterceptor
{
    private const string UseDbContext = "UseDbContext";
    private const string UsePaging = "UsePaging";
    private const string UseProjection = "UseProjection";
    private const string UseFiltering = "UseFiltering";
    private const string UseSorting = "UseSorting";

    private readonly HashSet<string> _names = [];

    public override void OnAfterCompleteType(
        ITypeCompletionContext completionContext,
        TypeSystemConfiguration configuration)
    {
        if (completionContext.DescriptorContext.Options.ValidatePipelineOrder
            && configuration is ObjectTypeConfiguration objectTypeDef)
        {
            foreach (var field in objectTypeDef.Fields)
            {
                if (field.MiddlewareConfigurations.Count > 1)
                {
                    ValidatePipeline(
                        completionContext.Type,
                        new SchemaCoordinate(completionContext.Type.Name, field.Name),
                        field.MiddlewareConfigurations);
                }
            }
        }
    }

    private void ValidatePipeline(
        TypeSystemObject type,
        SchemaCoordinate fieldCoordinate,
        IList<FieldMiddlewareConfiguration> middlewareDefinitions)
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
                            (duplicates ??= []).Add(UseDbContext);
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
                            (duplicates ??= []).Add(UsePaging);
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
                            (duplicates ??= []).Add(UseProjection);
                        }

                        useProjections = true;
                        break;

                    case WellKnownMiddleware.Filtering:
                        if (!_names.Add(definition.Key))
                        {
                            (duplicates ??= []).Add(UseFiltering);
                        }
                        useFiltering = true;
                        break;

                    case WellKnownMiddleware.Sorting:
                        if (!_names.Add(definition.Key))
                        {
                            (duplicates ??= []).Add(UseSorting);
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
        IList<FieldMiddlewareConfiguration> middlewareDefinitions)
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
                        sb.Append(UseDbContext);
                        break;

                    case WellKnownMiddleware.Paging:
                        other = false;
                        PrintNext();
                        sb.Append(UsePaging);
                        break;

                    case WellKnownMiddleware.Projection:
                        other = false;
                        PrintNext();
                        sb.Append(UseProjection);
                        break;

                    case WellKnownMiddleware.Filtering:
                        other = false;
                        PrintNext();
                        sb.Append(UseFiltering);
                        break;

                    case WellKnownMiddleware.Sorting:
                        other = false;
                        PrintNext();
                        sb.Append(UseSorting);
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
