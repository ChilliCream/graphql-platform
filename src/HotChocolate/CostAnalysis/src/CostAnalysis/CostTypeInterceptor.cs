using System.Collections.Immutable;
using HotChocolate.Configuration;
using HotChocolate.CostAnalysis.Types;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Helpers;
using HotChocolate.Types.Pagination;
using Microsoft.Extensions.DependencyInjection;
using static HotChocolate.Types.Pagination.PagingDefaults;

namespace HotChocolate.CostAnalysis;

internal sealed class CostTypeInterceptor : TypeInterceptor
{
    private readonly ImmutableArray<string> _forwardAndBackwardSlicingArgs
        = ImmutableArray.Create<string>("first", "last");

    private readonly ImmutableArray<string> _forwardSlicingArgs
        = ImmutableArray.Create<string>("first");

    private readonly ImmutableArray<string> _sizedFields
        = ImmutableArray.Create<string>("edges", "nodes");

    private readonly ImmutableArray<string> _offSetSlicingArgs
        = ImmutableArray.Create<string>("take");

    private readonly ImmutableArray<string> _offsetSizedFields
        = ImmutableArray.Create<string>("items");

    private CostOptions _options = default!;

    internal override uint Position => int.MaxValue;

    internal override void InitializeContext(
        IDescriptorContext context,
        TypeInitializer typeInitializer,
        TypeRegistry typeRegistry,
        TypeLookup typeLookup,
        TypeReferenceResolver typeReferenceResolver)
    {
        _options = context.Services.GetRequiredService<CostOptions>();
    }

    public override void OnAfterCompleteName(ITypeCompletionContext completionContext, DefinitionBase definition)
    {
        if (definition is ObjectTypeDefinition objectTypeDef)
        {
            foreach (var fieldDef in objectTypeDef.Fields)
            {
                if (fieldDef.State.Count > 0
                    && fieldDef.State.TryGetValue(WellKnownContextData.PagingOptions, out var value)
                    && value is PagingOptions options
                    && !fieldDef.HasListSizeDirective()
                    && ((fieldDef.Flags & FieldFlags.Connection) == FieldFlags.Connection
                        || (fieldDef.Flags & FieldFlags.CollectionSegment) == FieldFlags.CollectionSegment))
                {
                    var assumedSize = options.MaxPageSize ?? MaxPageSize;

                    var slicingArgs =
                        (fieldDef.Flags & FieldFlags.Connection) == FieldFlags.Connection
                            ? options.AllowBackwardPagination ?? AllowBackwardPagination
                                ? _forwardAndBackwardSlicingArgs
                                : _forwardSlicingArgs
                            : _offSetSlicingArgs;

                    var sizeFields =
                        (fieldDef.Flags & FieldFlags.Connection) == FieldFlags.Connection
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

                    fieldDef.AddDirective(
                        new ListSizeDirective(
                            assumedSize,
                            slicingArgs,
                            sizeFields,
                            requirePagingBoundaries),
                        completionContext.DescriptorContext.TypeInspector);
                }

                foreach (var argumentDef in fieldDef.Arguments)
                {
                    if ((argumentDef.Flags & FieldFlags.FilterArgument) == FieldFlags.FilterArgument
                        && _options.Sorting.DefaultSortArgumentCost.HasValue
                        && !fieldDef.HasCostDirective())
                    {
                        argumentDef.AddDirective(
                            new CostDirective(_options.Sorting.DefaultSortArgumentCost.Value),
                            completionContext.DescriptorContext.TypeInspector);
                    }
                    else if ((argumentDef.Flags & FieldFlags.SortArgument) == FieldFlags.SortArgument
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

        if (definition is InputObjectTypeDefinition inputObjectTypeDef)
        {
            foreach (var fieldDef in inputObjectTypeDef.Fields)
            {
                if ((fieldDef.Flags & FieldFlags.FilterOperationField) == FieldFlags.FilterOperationField
                    && _options.Filtering.DefaultFilterOperationCost.HasValue
                    && !fieldDef.HasCostDirective())
                {
                    fieldDef.AddDirective(
                        new CostDirective(_options.Filtering.DefaultFilterOperationCost.Value),
                        completionContext.DescriptorContext.TypeInspector);
                }
                else if ((fieldDef.Flags & FieldFlags.FilterExpensiveOperationField)
                    == FieldFlags.FilterExpensiveOperationField
                    && _options.Filtering.DefaultExpensiveFilterOperationCost.HasValue
                    && !fieldDef.HasCostDirective())
                {
                    fieldDef.AddDirective(
                        new CostDirective(_options.Filtering.DefaultExpensiveFilterOperationCost.Value),
                        completionContext.DescriptorContext.TypeInspector);
                }
                else if ((fieldDef.Flags & FieldFlags.SortOperationField) == FieldFlags.SortOperationField
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

    public override void OnBeforeCompleteType(ITypeCompletionContext completionContext, DefinitionBase definition)
    {
        if (definition is ObjectTypeDefinition objectTypeDef)
        {
            foreach (var fieldDef in objectTypeDef.Fields)
            {
                if ((fieldDef.PureResolver is null
                        || (fieldDef.Flags & FieldFlags.TotalCount) == FieldFlags.TotalCount)
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

file static class Extensions
{
    public static bool HasCostDirective(this IHasDirectiveDefinition directiveProvider)
        => directiveProvider.Directives.Any(
            t => t.Value is CostDirective or DirectiveNode { Name.Value: "cost" });

    public static bool HasListSizeDirective(this IHasDirectiveDefinition directiveProvider)
        => directiveProvider.Directives.Any(
            t => t.Value is ListSizeDirective or DirectiveNode { Name.Value: "listSize" });
}
