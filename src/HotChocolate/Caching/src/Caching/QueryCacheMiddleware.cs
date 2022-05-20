using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.Types;
using static HotChocolate.Caching.WellKnownContextData;

namespace HotChocolate.Caching;

public sealed class QueryCacheMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IQueryCache[] _caches;
    private readonly ICacheControlOptions _options;

    public QueryCacheMiddleware(
        RequestDelegate next,
        [SchemaService] IEnumerable<IQueryCache> caches,
        [SchemaService] ICacheControlOptionsAccessor optionsAccessor)
    {
        _next = next;
        _caches = caches.ToArray();
        _options = optionsAccessor.CacheControl;
    }

    public async ValueTask InvokeAsync(IRequestContext context)
    {
        if (!_options.Enable || context.ContextData.ContainsKey(SkipQueryCaching))
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        // Since we are only "writing" to a cache using HTTP Cache-Control,
        // we do not yet have to worry about the details for reading from
        // a user-space cache implementation.

        //foreach (IQueryCache cache in _caches)
        //{
        //    try
        //    {
        //        if (!cache.ShouldReadResultFromCache(context))
        //        {
        //            continue;
        //        }

        //        IQueryResult? cachedResult =
        //            await cache.TryReadCachedQueryResultAsync(context, _options);

        //        if (cachedResult is not null)
        //        {
        //            context.Result = cachedResult;
        //            return;
        //        }
        //    }
        //    catch
        //    {
        //        // An exception while trying to retrieve the cached query result
        //        // should not error out the actual query, so we are ignoring it.
        //    }
        //}

        await _next(context).ConfigureAwait(false);

        if (context.DocumentId is null || context.Operation is null)
        {
            return;
        }

        var result = new CacheControlResult();

        try
        {
            IPreparedOperation operation = context.Operation;
            IReadOnlyList<ISelection> rootSelections =
                operation.GetRootSelectionSet().Selections;

            foreach (ISelection rootSelection in rootSelections)
            {
                ProcessSelection(rootSelection, result, operation);
            }

            if (!result.MaxAge.HasValue)
            {
                // No field in the query specified a maxAge value,
                // so we do not attempt to cache it.
                return;
            }

            foreach (IQueryCache cache in _caches)
            {
                try
                {
                    if (!cache.ShouldWriteResultToCache(context))
                    {
                        continue;
                    }

                    await cache.CacheQueryResultAsync(context,
                        result, _options);
                }
                catch
                {
                    // An exception while trying to cache the query result
                    // should not error out the actual query, so we are ignoring it.
                }
            }
        }
        catch
        {
            // An exception during the calculation of the CacheControlResult
            // should not error out the actual query, so we are ignoring it.
        }
    }

    private static void ProcessSelection(ISelection selection,
        CacheControlResult result, IPreparedOperation operation)
    {
        IObjectField field = selection.Field;

        CacheControlDirective? directive = field.Directives
            .FirstOrDefault(d => d.Name == "cacheControl")?
            .ToObject<CacheControlDirective>();

        var maxAgeSet = false;
        var scopeSet = false;

        if (directive is not null)
        {
            // The @cacheControl directive was specified on this field.

            if (directive.MaxAge.HasValue &&
                (!result.MaxAge.HasValue ||
                    directive.MaxAge < result.MaxAge.Value))
            {
                // The maxAge of the @cacheControl on this field is lower
                // than the previously lowest maxAge value.
                result.MaxAge = directive.MaxAge.Value;
                maxAgeSet = true;
            }
            else if (directive.InheritMaxAge == true)
            {
                // If inheritMaxAge is set, we keep the computed maxAge value as is.
                maxAgeSet = true;
            }

            if (directive.Scope.HasValue &&
                directive.Scope < result.Scope)
            {
                // The scope of the @cacheControl on this field is more restrivive
                // than the computed scope.
                result.Scope = directive.Scope.Value;
                scopeSet = true;
            }
        }

        if (!maxAgeSet || !scopeSet)
        {
            // Either maxAge or scope have not been specified by the @cacheControl
            // directive on the field, so we try to infer these details
            // from the type of the field.

            // todo: this might not contain union types
            if (field.Type is IComplexOutputType type)
            {
                // The type of the field is complex and can therefore be
                // annotated with a @cacheControl directive.

                directive = type.Directives
                    .FirstOrDefault(d => d.Name == "cacheControl")?
                    .ToObject<CacheControlDirective>();

                if (directive is not null)
                {
                    // The @cacheControl directive was specified on this type.

                    if (!maxAgeSet && directive.MaxAge.HasValue &&
                        (!result.MaxAge.HasValue
                            || directive.MaxAge < result.MaxAge.Value))
                    {
                        // The field did not specify a value for maxAge and the
                        // maxAge of the @cacheControl directive on this type 
                        // is lower than the previously lowest maxAge value.
                        result.MaxAge = directive.MaxAge.Value;
                    }

                    if (!scopeSet && directive.Scope.HasValue &&
                        directive.Scope < result.Scope)
                    {
                        // The field did not specify a value for scope and the
                        // scope of the @cacheControl directive on this type 
                        // is more restrictive than the computed scope.
                        result.Scope = directive.Scope.Value;
                    }
                }
            }
        }

        SelectionSetNode? childSelection = selection.SelectionSet;

        if (childSelection is null)
        {
            // No fields are selected below the current field.
            return;
        }

        IEnumerable<IObjectType> possibleTypes =
            operation.GetPossibleTypes(childSelection);

        foreach (IObjectType type in possibleTypes)
        {
            IReadOnlyList<ISelection> typeSet =
                operation.GetSelectionSet(childSelection, type).Selections;

            foreach (ISelection typeSelection in typeSet)
            {
                ProcessSelection(typeSelection, result, operation);
            }
        }
    }
}
