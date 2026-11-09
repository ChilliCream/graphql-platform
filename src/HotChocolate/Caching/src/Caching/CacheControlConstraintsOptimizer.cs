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
internal sealed class CacheControlConstraintsOptimizer(
    ICacheControlOptionsAccessor optionsAccessor)
    : IOperationOptimizer
{
    private readonly ICacheControlOptions _options = optionsAccessor.CacheControl;

    public void OptimizeOperation(OperationOptimizerContext context)
    {
        if (context.Definition.Operation is not OperationType.Query ||
            context.HasIncrementalParts ||
            ContainsIntrospectionFields(context))
        {
            // if this is an introspection query we will not cache it.
            return;
        }

        var defaults = _options is { Enable: true, ApplyDefaults: true } ? _options : null;
        var constraints = ComputeCacheControlConstraints(context.CreateOperation(), defaults);

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
        IOperation operation,
        ICacheControlOptions? defaults)
    {
        var constraints = new CacheControlConstraints();
        var rootSelections = operation.RootSelectionSet.Selections;

        foreach (var rootSelection in rootSelections)
        {
            ProcessSelection(rootSelection, constraints, operation, defaults);
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

    private static CacheControlDirective? GetCacheControlDirective(IDirectiveCollection directives)
        => directives
            .FirstOrDefault(CacheControlDirectiveType.Names.DirectiveName)?
            .AsValue<CacheControlDirective>();

    private static bool DeclaresLifetime(CacheControlDirective? directive)
        => directive is not null
            && (directive.MaxAge.HasValue
                || directive.SharedMaxAge.HasValue
                || directive.InheritMaxAge == true);

    private static void ProcessSelection(
        ISelection selection,
        CacheControlConstraints constraints,
        IOperation operation,
        ICacheControlOptions? defaults)
    {
        var field = selection.Field;
        var returnType = field.Type.NamedType();
        var maxAgeSet = false;
        var sharedMaxAgeSet = false;
        var scopeSet = false;
        var varySet = false;

        // A root field without @cacheControl, or whose directive and return type set no
        // lifetime, takes the default lifetime, and the default scope unless its directive
        // sets one.
        if (defaults is not null && !field.IsIntrospectionField)
        {
            var fieldDirective = GetCacheControlDirective(field.Directives);

            if (fieldDirective is null
                || (!DeclaresLifetime(fieldDirective)
                    && !ReturnTypeDeclaresLifetime(selection, returnType, operation)))
            {
                ApplyCacheControl(
                    defaults.DefaultMaxAge,
                    directiveSharedMaxAge: null,
                    directiveInheritMaxAge: null,
                    fieldDirective?.Scope is null ? defaults.DefaultScope : null,
                    directiveVary: null);
            }
        }

        ExtractCacheControlDetailsFromDirectives(field.Directives);

        var maxAgeSetByField = maxAgeSet;
        var sharedMaxAgeSetByField = sharedMaxAgeSet;

        if (!maxAgeSet || !sharedMaxAgeSet || !scopeSet || !varySet)
        {
            // Either maxAge or scope have not been specified by the @cacheControl
            // directive on the field, so we try to infer these details
            // from the type of the field.

            if (returnType is IHasDirectives type)
            {
                // The type of the field is complex and can therefore be
                // annotated with a @cacheControl directive.
                ExtractCacheControlDetailsFromDirectives(type.Directives);
            }
        }

        if (selection.SelectionSet is not null)
        {
            var isAbstractType = returnType.IsAbstractType();
            var possibleTypes = operation.GetPossibleTypes(selection);

            foreach (var type in possibleTypes)
            {
                if (isAbstractType)
                {
                    maxAgeSet = maxAgeSetByField;
                    sharedMaxAgeSet = sharedMaxAgeSetByField;
                    ExtractCacheControlDetailsFromDirectives(type.Directives);
                }

                var selectionSet = Unsafe.As<SelectionSet>(operation.GetSelectionSet(selection, type));
                var length = selectionSet.Selections.Count;
                ref var start = ref selectionSet.GetSelectionsReference();

                for (var i = 0; i < length; i++)
                {
                    ProcessSelection(
                        Unsafe.Add(ref start, i),
                        constraints,
                        operation,
                        defaults: null);
                }
            }
        }

        void ExtractCacheControlDetailsFromDirectives(
            IDirectiveCollection directives)
        {
            var directive = GetCacheControlDirective(directives);

            if (directive is null)
            {
                return;
            }

            ApplyCacheControl(
                directive.MaxAge,
                directive.SharedMaxAge,
                directive.InheritMaxAge,
                directive.Scope,
                directive.Vary);
        }

        void ApplyCacheControl(
            int? directiveMaxAge,
            int? directiveSharedMaxAge,
            bool? directiveInheritMaxAge,
            CacheControlScope? directiveScope,
            ImmutableArray<string>? directiveVary)
        {
            var previousMaxAge = constraints.MaxAge;
            if (!maxAgeSet &&
                directiveMaxAge.HasValue)
            {
                // If only max-age has been set, we honor the expected behavior that a CDN
                // cannot ever cache longer than this unless s-maxage specifies otherwise.
                if (!constraints.MaxAge.HasValue || directiveMaxAge < constraints.MaxAge.Value)
                {
                    constraints.MaxAge = directiveMaxAge.Value;
                }

                if (!directiveSharedMaxAge.HasValue &&
                    constraints.SharedMaxAge.HasValue &&
                    constraints.SharedMaxAge.Value > directiveMaxAge.Value)
                {
                    constraints.SharedMaxAge = directiveMaxAge;
                }

                maxAgeSet = true;
            }
            else if (directiveInheritMaxAge == true)
            {
                // If inheritMaxAge is set, we keep the
                // computed maxAge value as is.
                maxAgeSet = true;
            }

            if (!sharedMaxAgeSet &&
                directiveSharedMaxAge.HasValue &&
                (!constraints.SharedMaxAge.HasValue || directiveSharedMaxAge < constraints.SharedMaxAge.Value))
            {
                // The maxAge of the @cacheControl directive is lower
                // than the previously lowest maxAge value.
                if (!constraints.SharedMaxAge.HasValue &&
                    previousMaxAge.HasValue &&
                    previousMaxAge.Value < directiveSharedMaxAge.Value)
                {
                    // If only max-age has been set, we honor the expected behavior that a CDN
                    // cannot ever cache longer than this unless s-maxage specifies otherwise.
                    constraints.SharedMaxAge = previousMaxAge.Value;
                }
                else
                {
                    constraints.SharedMaxAge = directiveSharedMaxAge.Value;
                }

                sharedMaxAgeSet = true;
            }
            else if (directiveInheritMaxAge == true)
            {
                // If inheritMaxAge is set, we keep the
                // computed maxAge value as is.
                sharedMaxAgeSet = true;
            }

            if (directiveScope.HasValue &&
                directiveScope < constraints.Scope)
            {
                // The scope of the @cacheControl directive is more
                // restrictive than the computed scope.
                constraints.Scope = directiveScope.Value;
                scopeSet = true;
            }

            if (directiveVary is { Length: > 0 })
            {
                constraints.Vary ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var value in directiveVary)
                {
                    constraints.Vary.Add(value);
                }

                varySet = true;
            }
        }
    }

    private static bool ReturnTypeDeclaresLifetime(
        ISelection selection,
        INamedType returnType,
        IOperation operation)
    {
        if (returnType is IHasDirectives type
            && DeclaresLifetime(GetCacheControlDirective(type.Directives)))
        {
            return true;
        }

        if (!returnType.IsAbstractType())
        {
            return false;
        }

        foreach (var possibleType in operation.GetPossibleTypes(selection))
        {
            if (!DeclaresLifetime(GetCacheControlDirective(possibleType.Directives)))
            {
                return false;
            }
        }

        return true;
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
