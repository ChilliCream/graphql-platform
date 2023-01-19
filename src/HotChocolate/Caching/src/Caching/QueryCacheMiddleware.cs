using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Introspection;
using static HotChocolate.Caching.WellKnownContextData;

namespace HotChocolate.Caching;

internal sealed class QueryCacheMiddleware
{
    private readonly RequestDelegate _next;
    private readonly QueryCache[] _caches;
    private readonly ICacheControlOptions _options;

    public QueryCacheMiddleware(
        RequestDelegate next,
        [SchemaService] IEnumerable<QueryCache> caches,
        [SchemaService] ICacheControlOptionsAccessor optionsAccessor)
    {
        _next = next;
        _caches = caches.ToArray();
        _options = optionsAccessor.CacheControl;
    }

    public async ValueTask InvokeAsync(IRequestContext context)
    {
        await _next(context).ConfigureAwait(false);

        if (!_options.Enable || context.ContextData.ContainsKey(SkipQueryCaching))
        {
            // If query caching is disabled or skipped,
            // we do not attempt to cache the result.
            return;
        }

        if (!CanOperationResultBeCached(context))
        {
            return;
        }

        var constraints = ComputeCacheControlConstraints(context.Operation);

        if (constraints is null || !constraints.MaxAge.HasValue)
        {
            // No field in the query specified a maxAge value,
            // so we do not attempt to cache it.
            return;
        }

        foreach (var cache in _caches)
        {
            if (!cache.ShouldWriteQueryResultToCache(context))
            {
                continue;
            }

            await cache.WriteQueryResultToCacheAsync(context, constraints, _options);
        }
    }

    private static bool CanOperationResultBeCached(IRequestContext context)
    {
        if (context.Result is not IQueryResult queryResult)
        {
            // Result is potentially deferred or batched,
            // we can not cache the entire query.
            return false;
        }

        if (context.Operation?.Definition.Operation != OperationType.Query)
        {
            // Request is not a query, so we do not cache it.
            return false;
        }

        if (queryResult.Errors is { Count: > 0 })
        {
            // Result has unexpected errors, we do not want to cache it.
            return false;
        }

        return true;
    }

    private static CacheControlConstraints? ComputeCacheControlConstraints(
        IOperation? operation)
    {
        if (operation is null)
        {
            return null;
        }

        var constraints = new CacheControlConstraints();

        var rootSelections = operation.RootSelectionSet.Selections;

        try
        {
            foreach (var rootSelection in rootSelections)
            {
                ProcessSelection(rootSelection, constraints, operation);
            }
        }
        catch (EncounteredIntrospectionFieldException)
        {
            // The operation specified introspection fields and should
            // therefore not be cached.
            return null;
        }

        return constraints;
    }

    private static void ProcessSelection(
        ISelection selection,
        CacheControlConstraints constraints,
        IOperation operation)
    {
        var field = selection.Field;

        if (field.IsIntrospectionField && field.Name != IntrospectionFields.TypeName)
        {
            // If we encounter an introspection field, we immediately stop
            // trying to compute the cache constraints.
            throw ThrowHelper.EncounteredIntrospectionField();
        }

        var maxAgeSet = false;
        var scopeSet = false;

        ExtractCacheControlDetailsFromDirectives(field.Directives);

        if (!maxAgeSet || !scopeSet)
        {
            // Either maxAge or scope have not been specified by the @cacheControl
            // directive on the field, so we try to infer these details
            // from the type of the field.

            if (field.Type is Types.IHasDirectives type)
            {
                // The type of the field is complex and can therefore be
                // annotated with a @cacheControl directive.

                ExtractCacheControlDetailsFromDirectives(type.Directives);
            }
        }

        try
        {
            // todo: this seems to be the only usage of this API - is there a better approach?
            var possibleTypes = operation.GetPossibleTypes(selection);

            foreach (var type in possibleTypes)
            {
                var typeSet = operation.GetSelectionSet(selection, type).Selections;

                foreach (var typeSelection in typeSet)
                {
                    ProcessSelection(typeSelection, constraints, operation);
                }
            }
        }
        catch
        {

        }

        void ExtractCacheControlDetailsFromDirectives(
            IDirectiveCollection directives)
        {
            var directive = directives
                .FirstOrDefault(CacheControlDirectiveType.DirectiveName)?
                .AsValue<CacheControlDirective>();

            if (directive is not null)
            {
                if (!maxAgeSet && directive.MaxAge.HasValue &&
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
                    // restrivive than the computed scope.
                    constraints.Scope = directive.Scope.Value;
                    scopeSet = true;
                }
            }
        }
    }
}
