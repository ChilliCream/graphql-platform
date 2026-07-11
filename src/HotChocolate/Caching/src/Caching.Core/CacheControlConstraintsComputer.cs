using System.Collections.Immutable;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Types;
using Microsoft.Net.Http.Headers;

namespace HotChocolate.Caching;

/// <summary>
/// Computes the cache control constraints for an operation by walking
/// the selection set and reading @cacheControl directives from the type system abstractions.
/// </summary>
internal static class CacheControlConstraintsComputer
{
    private const string DirectiveName = "cacheControl";
    private const string MaxAgeArg = "maxAge";
    private const string SharedMaxAgeArg = "sharedMaxAge";
    private const string InheritMaxAgeArg = "inheritMaxAge";
    private const string ScopeArg = "scope";
    private const string VaryArg = "vary";

    /// <summary>
    /// Computes the cache constraints for the given operation.
    /// Returns null if the operation is not a query or no constraints apply.
    /// </summary>
    public static ImmutableCacheConstraints? Compute(IOperation operation)
    {
        if (operation.Definition.Operation is not OperationType.Query)
        {
            return null;
        }

        var constraints = new CacheControlConstraints();
        var rootSelections = operation.RootSelectionSet.GetSelections();

        foreach (var rootSelection in rootSelections)
        {
            if (rootSelection.Field.IsIntrospectionField
                && !rootSelection.Field.Name.Equals("__typename", StringComparison.Ordinal))
            {
                // If this is an introspection query, we will not cache it.
                return null;
            }

            ProcessSelection(rootSelection, constraints, operation);
        }

        if (constraints.MaxAge is null && constraints.SharedMaxAge is null)
        {
            return null;
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

    /// <summary>
    /// Creates a <see cref="CacheControlHeaderValue"/> from the given constraints.
    /// </summary>
    public static CacheControlHeaderValue CreateHeaderValue(ImmutableCacheConstraints constraints)
    {
        return new CacheControlHeaderValue
        {
            Private = constraints.Scope == CacheControlScope.Private,
            MaxAge = constraints.MaxAge is not null
                ? TimeSpan.FromSeconds(constraints.MaxAge.Value)
                : null,
            SharedMaxAge = constraints.SharedMaxAge is not null
                ? TimeSpan.FromSeconds(constraints.SharedMaxAge.Value)
                : null
        };
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

        ExtractCacheControlFromDirectives(field);

        if (!maxAgeSet || !sharedMaxAgeSet || !scopeSet || !varySet)
        {
            // Either maxAge or scope have not been specified by the @cacheControl
            // directive on the field, so we try to infer these details
            // from the type of the field.

            if (field.Type is IDirectivesProvider type)
            {
                // The type of the field is complex and can therefore be
                // annotated with a @cacheControl directive.
                ExtractCacheControlFromDirectives(type);
            }
        }

        if (!selection.IsLeaf)
        {
            var possibleTypes = operation.GetPossibleTypes(selection);

            foreach (var type in possibleTypes)
            {
                var selectionSet = operation.GetSelectionSet(selection, type);
                var selections = selectionSet.GetSelections();

                foreach (var childSelection in selections)
                {
                    ProcessSelection(childSelection, constraints, operation);
                }
            }
        }

        void ExtractCacheControlFromDirectives(
            IDirectivesProvider typeSystemMember)
        {
            var directive = typeSystemMember.Directives.FirstOrDefault(DirectiveName);

            if (directive is null)
            {
                return;
            }

            var directiveMaxAge = GetIntArgument(directive, MaxAgeArg);
            var directiveSharedMaxAge = GetIntArgument(directive, SharedMaxAgeArg);
            var directiveInheritMaxAge = GetBoolArgument(directive, InheritMaxAgeArg);
            var directiveScope = GetScopeArgument(directive, ScopeArg);
            var directiveVary = GetStringListArgument(directive, VaryArg);

            var previousMaxAge = constraints.MaxAge;
            if (!maxAgeSet
                && directiveMaxAge.HasValue)
            {
                // If only max-age has been set, we honor the expected behavior that a CDN
                // cannot ever cache longer than this unless s-maxage specifies otherwise.
                if (!constraints.MaxAge.HasValue || directiveMaxAge < constraints.MaxAge.Value)
                {
                    constraints.MaxAge = directiveMaxAge.Value;
                }

                if (!directiveSharedMaxAge.HasValue
                    && constraints.SharedMaxAge.HasValue
                    && constraints.SharedMaxAge.Value > directiveMaxAge.Value)
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

            if (!sharedMaxAgeSet
                && directiveSharedMaxAge.HasValue
                && (!constraints.SharedMaxAge.HasValue || directiveSharedMaxAge < constraints.SharedMaxAge.Value))
            {
                // The maxAge of the @cacheControl directive is lower
                // than the previously lowest maxAge value.
                if (!constraints.SharedMaxAge.HasValue
                    && previousMaxAge.HasValue
                    && previousMaxAge.Value < directiveSharedMaxAge.Value)
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

            if (directiveScope.HasValue
                && directiveScope < constraints.Scope)
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

    private static int? GetIntArgument(IDirective directive, string name)
    {
        var value = directive.Arguments.GetValueOrDefault(name);

        if (value is IntValueNode intNode)
        {
            return intNode.ToInt32();
        }

        return null;
    }

    private static bool? GetBoolArgument(IDirective directive, string name)
    {
        var value = directive.Arguments.GetValueOrDefault(name);

        if (value is BooleanValueNode boolNode)
        {
            return boolNode.Value;
        }

        return null;
    }

    private static CacheControlScope? GetScopeArgument(IDirective directive, string name)
    {
        var value = directive.Arguments.GetValueOrDefault(name);

        if (value is EnumValueNode enumNode)
        {
            if (Enum.TryParse<CacheControlScope>(enumNode.Value, ignoreCase: true, out var scope))
            {
                return scope;
            }
        }

        return null;
    }

    private static string[]? GetStringListArgument(IDirective directive, string name)
    {
        var value = directive.Arguments.GetValueOrDefault(name);

        if (value is ListValueNode listNode && listNode.Items.Count > 0)
        {
            var result = new string[listNode.Items.Count];
            for (var i = 0; i < listNode.Items.Count; i++)
            {
                if (listNode.Items[i] is StringValueNode strNode)
                {
                    result[i] = strNode.Value;
                }
            }

            return result;
        }

        return null;
    }

    private sealed class CacheControlConstraints
    {
        public CacheControlScope Scope { get; set; } = CacheControlScope.Public;

        internal int? MaxAge { get; set; }

        internal int? SharedMaxAge { get; set; }

        internal HashSet<string>? Vary { get; set; }
    }
}
