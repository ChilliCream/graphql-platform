using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Validation;
using static HotChocolate.Caching.WellKnownContextData;

namespace HotChocolate.Caching;

public sealed class QueryCacheMiddleware
{
    private static readonly string _contextKey = nameof(CacheControlOptions);

    private readonly RequestDelegate _next;
    private readonly DocumentValidatorContextPool _contextPool;
    private readonly IQueryCache[] _caches;
    private readonly ICacheControlOptions _options;

    public QueryCacheMiddleware(
        RequestDelegate next,
        DocumentValidatorContextPool contextPool,
        IEnumerable<IQueryCache> caches)
    {
        _next = next;
        _contextPool = contextPool;
        _caches = caches.ToArray();

        // todo: how to properly access options in this middleware?
        //       schema service which accesses the context?
        _options = new CacheControlOptions();
    }

    public async ValueTask InvokeAsync(IRequestContext context)
    {
        if (!_options.Enable || context.ContextData.ContainsKey(SkipQueryCaching))
        {
            await _next(context).ConfigureAwait(false);
        }
        else
        {
            foreach (IQueryCache cache in _caches)
            {
                if (!cache.ShouldReadResultFromCache(context))
                {
                    continue;
                }

                // todo: new JSON stream implementation for IQueryResult
                IQueryResult? cachedResult =
                    await cache.TryReadCachedQueryResultAsync(context, _options);

                if (cachedResult is not null)
                {
                    context.Result = cachedResult;
                    return;
                }
            }

            await _next(context).ConfigureAwait(false);

            if (context.DocumentId is not null &&
                context.Operation is not null)
            {
                var result = new CacheControlResult();

                IPreparedOperation operation = context.Operation;
                IReadOnlyList<ISelection> rootSelections =
                    operation.GetRootSelectionSet().Selections;

                foreach (ISelection rootSelection in rootSelections)
                {
                    ProcessSelection(rootSelection, result, operation);
                }

                if (!result.MaxAge.HasValue)
                {
                    result.MaxAge = _options.DefaultMaxAge;
                }

                foreach (IQueryCache cache in _caches)
                {
                    if (!cache.ShouldWriteResultToCache(context))
                    {
                        continue;
                    }

                    await cache.CacheQueryResultAsync(context,
                        result, _options);
                }
            }
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
                // than the computed maxAge value.
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
                        // is lower than the computed maxAge value.
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