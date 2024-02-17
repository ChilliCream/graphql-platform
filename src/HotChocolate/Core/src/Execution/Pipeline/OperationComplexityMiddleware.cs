using System;
using System.Threading.Tasks;
using HotChocolate.Execution.Caching;
using HotChocolate.Execution.Options;
using HotChocolate.Execution.Pipeline.Complexity;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.Validation;
using Microsoft.Extensions.DependencyInjection;
using static HotChocolate.Execution.ErrorHelper;
using static HotChocolate.Execution.Pipeline.PipelineTools;
using static HotChocolate.WellKnownContextData;

namespace HotChocolate.Execution.Pipeline;

internal sealed class OperationComplexityMiddleware
{
    private readonly RequestDelegate _next;
    private readonly DocumentValidatorContextPool _contextPool;
    private readonly ComplexityAnalyzerSettings _settings;
    private readonly IComplexityAnalyzerCache _cache;
    private readonly ComplexityAnalyzerCompiler _compiler;
    private readonly VariableCoercionHelper _coercionHelper;

    private OperationComplexityMiddleware(
        RequestDelegate next,
        DocumentValidatorContextPool contextPool,
        [SchemaService] IComplexityAnalyzerOptionsAccessor options,
        IComplexityAnalyzerCache cache,
        VariableCoercionHelper coercionHelper)
    {
        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        _next = next ??
            throw new ArgumentNullException(nameof(next));
        _contextPool = contextPool ??
            throw new ArgumentNullException(nameof(contextPool));
        _settings = options.Complexity;
        _cache = cache ??
            throw new ArgumentNullException(nameof(cache));
        _coercionHelper = coercionHelper ??
            throw new ArgumentNullException(nameof(coercionHelper));

        _compiler = new ComplexityAnalyzerCompiler(_settings);
    }

    public async ValueTask InvokeAsync(IRequestContext context)
    {
        if (!_settings.Enable || context.ContextData.ContainsKey(SkipComplexityAnalysis))
        {
            await _next(context).ConfigureAwait(false);
        }
        else
        {
            if (context.DocumentId is not null &&
                context.OperationId is not null &&
                context.Document is not null)
            {
                var diagnostic = context.DiagnosticEvents;

                using (diagnostic.AnalyzeOperationComplexity(context))
                {
                    var document = context.Document;
                    var operationDefinition =
                        context.Operation?.Definition ??
                        document.GetOperation(context.Request.OperationName);

                    if (!_cache.TryGetAnalyzer(context.OperationId, out var analyzer))
                    {
                        analyzer = CompileAnalyzer(
                            context,
                            document,
                            operationDefinition,
                            context.OperationId);
                        diagnostic.OperationComplexityAnalyzerCompiled(context);
                    }

                    CoerceVariables(
                        context,
                        _coercionHelper,
                        operationDefinition.VariableDefinitions);

                    var complexity = analyzer(context.Services, context.Variables!);
                    var allowedComplexity = GetMaximumAllowedComplexity(context);
                    context.ContextData[_settings.ContextDataKey] = complexity;
                    diagnostic.OperationComplexityResult(context, complexity, allowedComplexity);

                    if (complexity <= allowedComplexity)
                    {
                        await _next(context).ConfigureAwait(false);
                    }
                    else
                    {
                        context.Result = MaxComplexityReached(complexity, _settings.MaximumAllowed);
                    }
                }
            }
            else
            {
                context.Result = StateInvalidForComplexityAnalyzer();
            }
        }
    }

    private ComplexityAnalyzerDelegate CompileAnalyzer(
        IRequestContext requestContext,
        DocumentNode document,
        OperationDefinitionNode operationDefinition,
        string operationId)
    {
        var validatorContext = _contextPool.Get();

        try
        {
            PrepareContext(requestContext, document, validatorContext);
            _compiler.Visit(operationDefinition, validatorContext);
            var analyzer = (OperationComplexityAnalyzer)validatorContext.List.Pop()!;
            _cache.TryAddAnalyzer(operationId, analyzer.Analyzer);
            return analyzer.Analyzer;
        }
        finally
        {
            validatorContext.Clear();
            _contextPool.Return(validatorContext);
        }
    }

    private static void PrepareContext(
        IRequestContext requestContext,
        DocumentNode document,
        DocumentValidatorContext validatorContext)
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

    private int GetMaximumAllowedComplexity(IRequestContext requestContext)
    {
        if (requestContext.ContextData.TryGetValue(MaximumAllowedComplexity, out var value) &&
            value is int allowedComplexity)
        {
            return allowedComplexity;
        }

        return _settings.MaximumAllowed;
    }
    
    public static RequestCoreMiddleware Create()
        => (core, next) =>
        {
            var contextPool = core.Services.GetRequiredService<DocumentValidatorContextPool>();
            var options = core.SchemaServices.GetRequiredService<IRequestExecutorOptionsAccessor>();
            var cache = core.Services.GetRequiredService<IComplexityAnalyzerCache>();
            var coercionHelper = core.Services.GetRequiredService<VariableCoercionHelper>();
            var middleware = new OperationComplexityMiddleware(next, contextPool, options, cache, coercionHelper);
            return context => middleware.InvokeAsync(context);
        };
}
