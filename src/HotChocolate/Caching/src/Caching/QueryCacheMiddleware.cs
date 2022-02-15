using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Caching;

public sealed class QueryCacheMiddleware
{
    private static readonly string _contextKey = nameof(CacheControlOptions);

    private readonly RequestDelegate _next;
    private readonly DocumentValidatorContextPool _contextPool;
    private readonly IQueryCache[] _caches;
    private readonly CacheControlValidatorVisitor _compiler;
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
        _compiler = new CacheControlValidatorVisitor();
    }

    public async ValueTask InvokeAsync(IRequestContext context)
    {
        // todo: add context key for skipping the query cache on a per-request basis
        if (!_options.Enable)
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

            // todo: implement
            //     if (context.DocumentId is not null &&
            //         context.Operation is not null)
            //     {
            //         IPreparedOperation operation = context.Operation;

            //         var set = operation.GetRootSelectionSet().Selections[0].SelectionSet;
            //         var type = operation.GetPossibleTypes(set).First();
            //         var set2 = operation.GetSelectionSet(set, type);

            //         // todo: try to get CacheControlResult from operation cache
            //         // var cacheId = context.CreateCacheId(context.OperationId);

            //         // todo: we do this computation without knowing 
            //         //       whether one of the caches actually wants to cache it...
            //         // CacheControlResult? result = ComputeCacheControlResult(context, document, operationDefinition);

            //         if (result is null)
            //         {
            //             return;
            //         }

            //         foreach (IQueryCache cache in _caches)
            //         {
            //             if (!cache.ShouldWriteResultToCache(context))
            //             {
            //                 continue;
            //             }

            //             await cache.CacheQueryResultAsync(context, result, _options);
            //         }
            //     }
        }
    }

    private CacheControlResult? ComputeCacheControlResult(IRequestContext context,
        DocumentNode document, OperationDefinitionNode operationDefinition)
    {
        DocumentValidatorContext validatorContext = _contextPool.Get();
        CacheControlResult? operationCacheControlResult = null;

        try
        {
            PrepareContext(context, document, validatorContext);

            _compiler.Visit(document, validatorContext);

            var cacheControlResults = (List<CacheControlResult>)validatorContext.List.Peek()!;

            foreach (CacheControlResult cacheControlResult in cacheControlResults)
            {
                if (!cacheControlResult.MaxAgeHasValue)
                {
                    cacheControlResult.MaxAge = _options.DefaultMaxAge;
                }

                if (cacheControlResult.OperationDefinitionNode == operationDefinition)
                {
                    operationCacheControlResult = cacheControlResult;
                }

                // todo: add to operation cache
                // var cacheId = context.CreateCacheId(
                //          CreateOperationId(
                //              context.DocumentId!,
                //              cacheControlResult.OperationDefinitionNode.Name?.Value));
            }
        }
        finally
        {
            validatorContext.Clear();
            _contextPool.Return(validatorContext);
        }

        // todo: handle dynamic cache hints set via IResolverContext

        return operationCacheControlResult!;
    }

    private static void PrepareContext(IRequestContext requestContext,
        DocumentNode document, DocumentValidatorContext validatorContext)
    {
        validatorContext.Schema = requestContext.Schema;

        for (var i = 0; i < document.Definitions.Count; i++)
        {
            if (document.Definitions[i] is FragmentDefinitionNode fragmentDefinition)
            {
                validatorContext.Fragments[fragmentDefinition.Name.Value] = fragmentDefinition;
            }
        }

        validatorContext.ContextData = requestContext.ContextData;
    }
}