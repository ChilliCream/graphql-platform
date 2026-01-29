using System.Collections.Immutable;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Utilities;
using Microsoft.Net.Http.Headers;

namespace HotChocolate.Caching;

/// <summary>
/// Computes the cache control constraints for an operation during compilation.
/// </summary>
internal sealed class CacheControlConstraintsOptimizer : IOperationOptimizer
{
    public void OptimizeOperation(OperationOptimizerContext context)
    {
        // TODO : we need to include this again when defer is back.
        if (context.Operation.Kind is not OperationType.Query
            // || context.HasIncrementalParts
            || ContainsIntrospectionFields(context))
        {
            // if this is an introspection query, we will not cache it.
            return;
        }

        var constraints = ComputeCacheControlConstraints(context.Operation);

        if (constraints.MaxAge is not null || constraints.SharedMaxAge is not null)
        {
            var headerValue = new CacheControlHeaderValue
            {
                Private = constraints.Scope == CacheControlScope.Private,
                MaxAge = constraints.MaxAge is not null
                    ? TimeSpan.FromSeconds(constraints.MaxAge.Value)
                    : null,
                SharedMaxAge = constraints.SharedMaxAge is not null
                    ? TimeSpan.FromSeconds(constraints.SharedMaxAge.Value)
                    : null
            };

            context.Operation.Features.SetSafe(constraints);
            context.Operation.Features.SetSafe(headerValue);
        }
    }

    private static ImmutableCacheConstraints ComputeCacheControlConstraints(
        Operation operation)
    {
        var constraints = new CacheControlConstraints();
        var rootSelections = operation.RootSelectionSet.Selections;

        foreach (var rootSelection in rootSelections)
        {
            ProcessSelection(rootSelection, constraints, operation);
        }

        ImmutableArray<string> vary;
        if (constraints.Vary is not null)
        {
            var builder = ImmutableArray.CreateBuilder<string>();

            foreach (var value in constraints.Vary.Order(StringComparer.OrdinalIgnoreCase))
            {
                builder.Add(value.ToLowerInvariant());
            }

            vary = builder.ToImmutable();
        }
        else
        {
            vary = [];
        }

        return new ImmutableCacheConstraints(
            constraints.MaxAge,
            constraints.SharedMaxAge,
            constraints.Scope,
            vary);
    }

    private static void ProcessSelection(
        Selection selection,
        CacheControlConstraints constraints,
        Operation operation)
    {
        var field = selection.Field;
        var maxAgeSet = false;
        var sharedMaxAgeSet = false;
        var scopeSet = false;
        var varySet = false;

        ExtractCacheControlDetailsFromDirectives(field);

        if (!maxAgeSet || !sharedMaxAgeSet || !scopeSet || !varySet)
        {
            // Either maxAge or scope have not been specified by the @cacheControl
            // directive on the field, so we try to infer these details
            // from the type of the field.

            if (field.Type is IDirectivesProvider type)
            {
                // The type of the field is complex and can therefore be
                // annotated with a @cacheControl directive.
                ExtractCacheControlDetailsFromDirectives(type);
            }
        }

        if (selection.HasSelections)
        {
            var possibleTypes = operation.GetPossibleTypes(selection);

            foreach (var type in possibleTypes)
            {
                var selectionSet = operation.GetSelectionSet(selection, type);
                var selections = selectionSet.Selections;

                foreach (var childSelection in selections)
                {
                    ProcessSelection(childSelection, constraints, operation);
                }
            }
        }

        void ExtractCacheControlDetailsFromDirectives(
            IDirectivesProvider typeSystemMember)
        {
            var directive = typeSystemMember.Directives.FirstOrDefaultValue<CacheControlDirective>(
                CacheControlDirectiveType.Names.DirectiveName);

            if (directive is not null)
            {
                var previousMaxAge = constraints.MaxAge;
                if (!maxAgeSet
                    && directive.MaxAge.HasValue)
                {
                    // If only max-age has been set, we honor the expected behavior that a CDN
                    // cannot ever cache longer than this unless s-maxage specifies otherwise.
                    if (!constraints.MaxAge.HasValue || directive.MaxAge < constraints.MaxAge.Value)
                    {
                        constraints.MaxAge = directive.MaxAge.Value;
                    }

                    if (!directive.SharedMaxAge.HasValue
                        && constraints.SharedMaxAge.HasValue
                        && constraints.SharedMaxAge.Value > directive.MaxAge.Value)
                    {
                        constraints.SharedMaxAge = directive.MaxAge;
                    }

                    maxAgeSet = true;
                }
                else if (directive.InheritMaxAge == true)
                {
                    // If inheritMaxAge is set, we keep the
                    // computed maxAge value as is.
                    maxAgeSet = true;
                }

                if (!sharedMaxAgeSet
                    && directive.SharedMaxAge.HasValue
                    && (!constraints.SharedMaxAge.HasValue || directive.SharedMaxAge < constraints.SharedMaxAge.Value))
                {
                    // The maxAge of the @cacheControl directive is lower
                    // than the previously lowest maxAge value.
                    if (!constraints.SharedMaxAge.HasValue
                        && previousMaxAge.HasValue
                        && previousMaxAge.Value < directive.SharedMaxAge.Value)
                    {
                        // If only max-age has been set, we honor the expected behavior that a CDN
                        // cannot ever cache longer than this unless s-maxage specifies otherwise.
                        constraints.SharedMaxAge = previousMaxAge.Value;
                    }
                    else
                    {
                        constraints.SharedMaxAge = directive.SharedMaxAge.Value;
                    }

                    sharedMaxAgeSet = true;
                }
                else if (directive.InheritMaxAge == true)
                {
                    // If inheritMaxAge is set, we keep the
                    // computed maxAge value as is.
                    sharedMaxAgeSet = true;
                }

                if (directive.Scope.HasValue
                    && directive.Scope < constraints.Scope)
                {
                    // The scope of the @cacheControl directive is more
                    // restrictive than the computed scope.
                    constraints.Scope = directive.Scope.Value;
                    scopeSet = true;
                }

                if (directive.Vary is { Length: > 0 })
                {
                    constraints.Vary ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                    foreach (var value in directive.Vary)
                    {
                        constraints.Vary.Add(value);
                    }

                    varySet = true;
                }
            }
        }
    }

    private static bool ContainsIntrospectionFields(OperationOptimizerContext context)
    {
        var selections = context.Operation.RootSelectionSet.Selections;

        foreach (var selection in selections)
        {
            var field = selection.Field;
            if (field.IsIntrospectionField
                && !field.Name.EqualsOrdinal(IntrospectionFieldNames.TypeName))
            {
                return true;
            }
        }

        return false;
    }

    private sealed class CacheControlConstraints
    {
        public CacheControlScope Scope { get; set; } = CacheControlScope.Public;

        internal int? MaxAge { get; set; }

        internal int? SharedMaxAge { get; set; }

        internal HashSet<string>? Vary { get; set; }
    }
}
