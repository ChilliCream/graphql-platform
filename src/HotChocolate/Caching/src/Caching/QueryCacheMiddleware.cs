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

public sealed class QueryCacheMiddleware
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

        var result = ComputeCacheControlResullt(context.Operation);

        if (result is null || !result.MaxAge.HasValue)
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

            await cache.WriteQueryResultToCacheAsync(context,
                result, _options);
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

    private static CacheControlResult? ComputeCacheControlResullt(
        IOperation? operation)
    {
        if (operation is null)
        {
            return null;
        }

        var result = new CacheControlResult();

        var rootSelections = operation.RootSelectionSet.Selections;

        try
        {
            foreach (var rootSelection in rootSelections)
            {
                ProcessSelection(rootSelection, result, operation);
            }
        }
        catch (EncounteredIntrospectionFieldException)
        {
            // The operation specified introspection fields and should
            // therefore not be cached.
            return null;
        }

        return result;
    }

    private static void ProcessSelection(ISelection selection,
        CacheControlResult result, IOperation operation)
    {
        var field = selection.Field;

        if (field.IsIntrospectionField && field.Name != IntrospectionFields.TypeName)
        {
            // If we encounter an introspection field, we immediately stop
            // trying to compute a cache control result.
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
                var typeSet = operation.GetSelectionSet(selection, type)
                    .Selections;

                foreach (var typeSelection in typeSet)
                {
                    ProcessSelection(typeSelection, result, operation);
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
                    .FirstOrDefault(d => d.Name == "cacheControl")?
                    .ToObject<CacheControlDirective>();

            if (directive is not null)
            {
                if (!maxAgeSet && directive.MaxAge.HasValue &&
                 (!result.MaxAge.HasValue ||
                     directive.MaxAge < result.MaxAge.Value))
                {
                    // The maxAge of the @cacheControl directive is lower
                    // than the previously lowest maxAge value.
                    result.MaxAge = directive.MaxAge.Value;
                    maxAgeSet = true;
                }
                else if (directive.InheritMaxAge == true)
                {
                    // If inheritMaxAge is set, we keep the
                    // computed maxAge value as is.
                    maxAgeSet = true;
                }

                if (directive.Scope.HasValue &&
                    directive.Scope < result.Scope)
                {
                    // The scope of the @cacheControl directive is more
                    // restrivive than the computed scope.
                    result.Scope = directive.Scope.Value;
                    scopeSet = true;
                }
            }
        }
    }
}
