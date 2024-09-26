using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Introspection;
using HotChocolate.Utilities;
using Microsoft.Net.Http.Headers;
using IHasDirectives = HotChocolate.Types.IHasDirectives;

namespace HotChocolate.Caching;

/// <summary>
/// Computes the cache control constraints for an operation during compilation.
/// </summary>
internal sealed class CacheControlConstraintsOptimizer : IOperationOptimizer
{
    public void OptimizeOperation(OperationOptimizerContext context)
    {
        if (context.Definition.Operation is not OperationType.Query ||
            context.HasIncrementalParts ||
            ContainsIntrospectionFields(context))
        {
            // if this is an introspection query we will not cache it.
            return;
        }

        var constraints = ComputeCacheControlConstraints(context.CreateOperation());

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
                    : null,
            };

            context.ContextData.Add(
                WellKnownContextData.CacheControlConstraints,
                constraints);

            context.ContextData.Add(
                WellKnownContextData.CacheControlHeaderValue,
                headerValue);
        }

        if (constraints.Vary is { Length: > 0 })
        {
            context.ContextData.Add(
                WellKnownContextData.VaryHeaderValue,
                string.Join(", ", constraints.Vary));
        }
    }

    private static ImmutableCacheConstraints ComputeCacheControlConstraints(
        IOperation operation)
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
            vary = ImmutableArray<string>.Empty;
        }

        return new ImmutableCacheConstraints(
            constraints.MaxAge,
            constraints.SharedMaxAge,
            constraints.Scope,
            vary);
    }

    private static void ProcessSelection(
        ISelection selection,
        CacheControlConstraints constraints,
        IOperation operation)
    {
        var field = selection.Field;
        var maxAgeSet = false;
        var sharedMaxAgeSet = false;
        var scopeSet = false;
        var varySet = false;

        ExtractCacheControlDetailsFromDirectives(field.Directives);

        if (!maxAgeSet || !sharedMaxAgeSet || !scopeSet || !varySet)
        {
            // Either maxAge or scope have not been specified by the @cacheControl
            // directive on the field, so we try to infer these details
            // from the type of the field.

            if (field.Type is IHasDirectives type)
            {
                // The type of the field is complex and can therefore be
                // annotated with a @cacheControl directive.
                ExtractCacheControlDetailsFromDirectives(type.Directives);
            }
        }

        if (selection.SelectionSet is not null)
        {
            var possibleTypes = operation.GetPossibleTypes(selection);

            foreach (var type in possibleTypes)
            {
                var selectionSet = Unsafe.As<SelectionSet>(operation.GetSelectionSet(selection, type));
                var length = selectionSet.Selections.Count;
                ref var start = ref selectionSet.GetSelectionsReference();

                for (var i = 0; i < length; i++)
                {
                    ProcessSelection(Unsafe.Add(ref start, i), constraints, operation);
                }
            }
        }

        void ExtractCacheControlDetailsFromDirectives(
            IDirectiveCollection directives)
        {
            var directive = directives
                .FirstOrDefault(CacheControlDirectiveType.Names.DirectiveName)?
                .AsValue<CacheControlDirective>();

            if (directive is not null)
            {
                var previousMaxAge = constraints.MaxAge;
                if (!maxAgeSet &&
                    directive.MaxAge.HasValue)
                {
                    // If only max-age has been set, we honor the expected behavior that a CDN
                    // cannot ever cache longer than this unless s-maxage specifies otherwise.
                    if (!constraints.MaxAge.HasValue || directive.MaxAge < constraints.MaxAge.Value)
                    {
                        constraints.MaxAge = directive.MaxAge.Value;
                    }

                    if (!directive.SharedMaxAge.HasValue &&
                        constraints.SharedMaxAge.HasValue &&
                        constraints.SharedMaxAge.Value > directive.MaxAge.Value)
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

                if (!sharedMaxAgeSet &&
                    directive.SharedMaxAge.HasValue &&
                    (!constraints.SharedMaxAge.HasValue || directive.SharedMaxAge < constraints.SharedMaxAge.Value))
                {
                    // The maxAge of the @cacheControl directive is lower
                    // than the previously lowest maxAge value.
                    if (!constraints.SharedMaxAge.HasValue &&
                        previousMaxAge.HasValue &&
                        previousMaxAge.Value < directive.SharedMaxAge.Value)
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

                if (directive.Scope.HasValue &&
                    directive.Scope < constraints.Scope)
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
        var length = context.RootSelectionSet.Selections.Count;
        ref var start = ref ((SelectionSet)context.RootSelectionSet).GetSelectionsReference();

        for (var i = 0; i < length; i++)
        {
            var field = Unsafe.Add(ref start, i).Field;

            if (field.IsIntrospectionField &&
                !field.Name.EqualsOrdinal(IntrospectionFields.TypeName))
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
