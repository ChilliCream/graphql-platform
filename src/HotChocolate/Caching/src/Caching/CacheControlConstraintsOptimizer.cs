using System.Runtime.CompilerServices;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Introspection;
using HotChocolate.Utilities;
using IHasDirectives = HotChocolate.Types.IHasDirectives;

namespace HotChocolate.Caching;

/// <summary>
/// Computes the cache control constraints for an operation during compilation.
/// </summary>
internal sealed class CacheControlConstraintsOptimizer : IOperationOptimizer
{
    private const string _cacheControlValueTemplate = "{0}, max-age={1}";
    private const string _cacheControlPrivateScope = "private";
    private const string _cacheControlPublicScope = "public";

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

        if (constraints.MaxAge is not null)
        {
            var cacheType = constraints.Scope switch
            {
                CacheControlScope.Private => _cacheControlPrivateScope,
                CacheControlScope.Public => _cacheControlPublicScope,
                _ => throw ThrowHelper.UnexpectedCacheControlScopeValue(constraints.Scope)
            };

            var headerValue = string.Format(
                _cacheControlValueTemplate,
                cacheType,
                constraints.MaxAge);

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
        IOperation operation)
    {
        var constraints = new CacheControlConstraints();
        var rootSelections = operation.RootSelectionSet.Selections;

        foreach (var rootSelection in rootSelections)
        {
            ProcessSelection(rootSelection, constraints, operation);
        }

        return constraints;
    }

    private static void ProcessSelection(
        ISelection selection,
        CacheControlConstraints constraints,
        IOperation operation)
    {
        var field = selection.Field;
        var maxAgeSet = false;
        var scopeSet = false;

        ExtractCacheControlDetailsFromDirectives(field.Directives);

        if (!maxAgeSet || !scopeSet)
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
                if (!maxAgeSet &&
                    directive.MaxAge.HasValue &&
                    (!constraints.MaxAge.HasValue || directive.MaxAge < constraints.MaxAge.Value))
                {
                    // The maxAge of the @cacheControl directive is lower
                    // than the previously lowest maxAge value.
                    constraints.MaxAge = directive.MaxAge.Value;
                    maxAgeSet = true;
                }
                else if (directive.InheritMaxAge == true)
                {
                    // If inheritMaxAge is set, we keep the
                    // computed maxAge value as is.
                    maxAgeSet = true;
                }

                if (directive.Scope.HasValue &&
                    directive.Scope < constraints.Scope)
                {
                    // The scope of the @cacheControl directive is more
                    // restrictive than the computed scope.
                    constraints.Scope = directive.Scope.Value;
                    scopeSet = true;
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
    }

}
