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

        if (constraints.MaxAge is not null)
        {
            var headerValue = new CacheControlHeaderValue
            {
                Private = constraints.Scope == CacheControlScope.Private,
                MaxAge = TimeSpan.FromSeconds(constraints.MaxAge.Value),
            };

            context.ContextData.Add(
                WellKnownContextData.CacheControlConstraints,
                new ImmutableCacheConstraints(
                    constraints.MaxAge.Value,
                    constraints.Scope));

            context.ContextData.Add(
                WellKnownContextData.CacheControlHeaderValue,
                headerValue);
        }
    }

    private static CacheControlConstraints ComputeCacheControlConstraints(
        IOperation operation,
        ICacheControlOptions? defaults)
    {
        var constraints = new CacheControlConstraints();
        var rootSelections = operation.RootSelectionSet.Selections;

        foreach (var rootSelection in rootSelections)
        {
            ProcessSelection(rootSelection, constraints, operation, defaults);
        }

        return constraints;
    }

    private static CacheControlDirective? GetCacheControlDirective(IDirectiveCollection directives)
        => directives
            .FirstOrDefault(CacheControlDirectiveType.Names.DirectiveName)?
            .AsValue<CacheControlDirective>();

    private static bool DeclaresLifetime(CacheControlDirective? directive)
        => directive is not null
            && (directive.MaxAge.HasValue || directive.InheritMaxAge == true);

    private static void ProcessSelection(
        ISelection selection,
        CacheControlConstraints constraints,
        IOperation operation,
        ICacheControlOptions? defaults)
    {
        var field = selection.Field;
        var returnType = field.Type.NamedType();
        var maxAgeSet = false;
        var scopeSet = false;

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
                    directiveInheritMaxAge: null,
                    fieldDirective?.Scope is null ? defaults.DefaultScope : null);
            }
        }

        ExtractCacheControlDetailsFromDirectives(field.Directives);

        var maxAgeSetByField = maxAgeSet;

        if (!maxAgeSet || !scopeSet)
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
                directive.InheritMaxAge,
                directive.Scope);
        }

        void ApplyCacheControl(
            int? directiveMaxAge,
            bool? directiveInheritMaxAge,
            CacheControlScope? directiveScope)
        {
            if (!maxAgeSet &&
                directiveMaxAge.HasValue &&
                (!constraints.MaxAge.HasValue || directiveMaxAge < constraints.MaxAge.Value))
            {
                // The maxAge of the @cacheControl directive is lower
                // than the previously lowest maxAge value.
                constraints.MaxAge = directiveMaxAge.Value;
                maxAgeSet = true;
            }
            else if (directiveInheritMaxAge == true)
            {
                // If inheritMaxAge is set, we keep the
                // computed maxAge value as is.
                maxAgeSet = true;
            }

            if (directiveScope.HasValue &&
                directiveScope < constraints.Scope)
            {
                // The scope of the @cacheControl directive is more
                // restrictive than the computed scope.
                constraints.Scope = directiveScope.Value;
                scopeSet = true;
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
    }
}
