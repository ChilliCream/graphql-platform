using System.Collections.Immutable;
using HotChocolate.Configuration;
using HotChocolate.CostAnalysis.Types;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Configurations;
using HotChocolate.Types.Helpers;
using HotChocolate.Types.Pagination;
using Microsoft.Extensions.DependencyInjection;
using static HotChocolate.Types.Pagination.PagingDefaults;

namespace HotChocolate.CostAnalysis;

internal sealed class CostTypeInterceptor : TypeInterceptor
{
    private readonly ImmutableArray<string> _forwardAndBackwardSlicingArgs
        = ["first", "last"];

    private readonly ImmutableArray<string> _forwardSlicingArgs
        = ["first"];

    private readonly ImmutableArray<string> _sizedFields
        = ["edges", "nodes"];

    private readonly ImmutableArray<string> _offSetSlicingArgs
        = ["take"];

    private readonly ImmutableArray<string> _offsetSizedFields
        = ["items"];

    private CostOptions _options = null!;

    internal override uint Position => int.MaxValue;

    public override bool IsEnabled(IDescriptorContext context)
        => context.Services.GetRequiredService<CostOptions>().ApplyCostDefaults;

    internal override void InitializeContext(
        IDescriptorContext context,
        TypeInitializer typeInitializer,
        TypeRegistry typeRegistry,
        TypeLookup typeLookup,
        TypeReferenceResolver typeReferenceResolver)
        => _options = context.Services.GetRequiredService<CostOptions>();

    public override void OnAfterCompleteName(ITypeCompletionContext completionContext, TypeSystemConfiguration configuration)
    {
        if (configuration is ObjectTypeConfiguration objectTypeDef)
        {
            foreach (var fieldDef in objectTypeDef.Fields)
            {
                if (fieldDef.Features.TryGet(out PagingOptions? options)
                    && !fieldDef.HasListSizeDirective()
                    && ((fieldDef.Flags & CoreFieldFlags.Connection) == CoreFieldFlags.Connection
                        || (fieldDef.Flags & CoreFieldFlags.CollectionSegment) == CoreFieldFlags.CollectionSegment))
                {
                    var assumedSize = options.MaxPageSize ?? MaxPageSize;

                    var slicingArgs =
                        (fieldDef.Flags & CoreFieldFlags.Connection) == CoreFieldFlags.Connection
                            ? options.AllowBackwardPagination ?? AllowBackwardPagination
                                ? _forwardAndBackwardSlicingArgs
                                : _forwardSlicingArgs
                            : _offSetSlicingArgs;

                    var sizeFields =
                        (fieldDef.Flags & CoreFieldFlags.Connection) == CoreFieldFlags.Connection
                            ? _sizedFields
                            : _offsetSizedFields;

                    // https://ibm.github.io/graphql-specs/cost-spec.html#sec-requireOneSlicingArgument
                    // Per default, requireOneSlicingArgument is enabled,
                    // and has to be explicitly disabled if not desired for a field.
                    // However, we have found that users turn the whole cost feature of because of this setting
                    // which leads to less overall security for the deployed GraphQL server.
                    // For this reason we have decided to disable slicing arguments by default.
                    var requirePagingBoundaries =
                        slicingArgs.Length > 0
                            && (options.RequirePagingBoundaries ?? false);

                    int? slicingArgumentDefaultValue = null;
                    if (_options.ApplySlicingArgumentDefaultValue)
                    {
                        slicingArgumentDefaultValue = options.DefaultPageSize ?? DefaultPageSize;
                    }

                    fieldDef.AddDirective(
                        new ListSizeDirective(
                            assumedSize,
                            slicingArgs,
                            sizeFields,
                            requirePagingBoundaries,
                            slicingArgumentDefaultValue),
                        completionContext.DescriptorContext.TypeInspector);
                }

                foreach (var argumentDef in fieldDef.Arguments)
                {
                    if ((argumentDef.Flags & CoreFieldFlags.FilterArgument) == CoreFieldFlags.FilterArgument
                        && _options.Sorting.DefaultSortArgumentCost.HasValue
                        && !fieldDef.HasCostDirective())
                    {
                        argumentDef.AddDirective(
                            new CostDirective(_options.Sorting.DefaultSortArgumentCost.Value),
                            completionContext.DescriptorContext.TypeInspector);
                    }
                    else if ((argumentDef.Flags & CoreFieldFlags.SortArgument) == CoreFieldFlags.SortArgument
                        && _options.Filtering.DefaultFilterArgumentCost.HasValue
                        && !fieldDef.HasCostDirective())
                    {
                        argumentDef.AddDirective(
                            new CostDirective(_options.Filtering.DefaultFilterArgumentCost.Value),
                            completionContext.DescriptorContext.TypeInspector);
                    }
                }
            }
        }

        if (configuration is InputObjectTypeConfiguration inputObjectTypeDef)
        {
            foreach (var fieldDef in inputObjectTypeDef.Fields)
            {
                if ((fieldDef.Flags & CoreFieldFlags.FilterOperationField) == CoreFieldFlags.FilterOperationField
                    && _options.Filtering.DefaultFilterOperationCost.HasValue
                    && !fieldDef.HasCostDirective())
                {
                    fieldDef.AddDirective(
                        new CostDirective(_options.Filtering.DefaultFilterOperationCost.Value),
                        completionContext.DescriptorContext.TypeInspector);
                }
                else if ((fieldDef.Flags & CoreFieldFlags.FilterExpensiveOperationField)
                    == CoreFieldFlags.FilterExpensiveOperationField
                    && _options.Filtering.DefaultExpensiveFilterOperationCost.HasValue
                    && !fieldDef.HasCostDirective())
                {
                    fieldDef.AddDirective(
                        new CostDirective(_options.Filtering.DefaultExpensiveFilterOperationCost.Value),
                        completionContext.DescriptorContext.TypeInspector);
                }
                else if ((fieldDef.Flags & CoreFieldFlags.SortOperationField) == CoreFieldFlags.SortOperationField
                    && _options.Sorting.DefaultSortOperationCost.HasValue
                    && !fieldDef.HasCostDirective())
                {
                    fieldDef.AddDirective(
                        new CostDirective(_options.Sorting.DefaultSortOperationCost.Value),
                        completionContext.DescriptorContext.TypeInspector);
                }
            }
        }
    }

    public override void OnBeforeCompleteType(ITypeCompletionContext completionContext, TypeSystemConfiguration configuration)
    {
        if (configuration is ObjectTypeConfiguration objectTypeDef)
        {
            foreach (var fieldDef in objectTypeDef.Fields)
            {
                if ((fieldDef.PureResolver is null
                        || (fieldDef.Flags & CoreFieldFlags.TotalCount) == CoreFieldFlags.TotalCount)
                    && _options.DefaultResolverCost.HasValue
                    && !fieldDef.HasCostDirective())
                {
                    fieldDef.AddDirective(
                        new CostDirective(_options.DefaultResolverCost.Value),
                        completionContext.DescriptorContext.TypeInspector);
                }
            }
        }
    }
}

internal sealed class CostDirectiveTypeInterceptor : TypeInterceptor
{
    internal override bool SkipDirectiveDefinition(DirectiveDefinitionNode node)
        => node.Name.Value.Equals("cost", StringComparison.Ordinal)
            || node.Name.Value.Equals("listSize", StringComparison.Ordinal);
}

file static class Extensions
{
    public static bool HasCostDirective(this IDirectiveConfigurationProvider directiveProvider)
        => directiveProvider.Directives.Any(
            t => t.Value is CostDirective or DirectiveNode { Name.Value: "cost" });

    public static bool HasListSizeDirective(this IDirectiveConfigurationProvider directiveProvider)
        => directiveProvider.Directives.Any(
            t => t.Value is ListSizeDirective or DirectiveNode { Name.Value: "listSize" });
}
