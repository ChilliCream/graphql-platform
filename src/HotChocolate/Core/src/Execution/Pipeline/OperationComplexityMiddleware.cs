using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Execution.Caching;
using HotChocolate.Execution.Options;
using HotChocolate.Execution.Pipeline.Complexity;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.Validation;
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
    private readonly ComplexityAnalyzerCompilerVisitor _compiler;
    private readonly VariableCoercionHelper _coercionHelper;

    public OperationComplexityMiddleware(
        RequestDelegate next,
        DocumentValidatorContextPool contextPool,
        IComplexityAnalyzerOptionsAccessor options,
        IComplexityAnalyzerCache cache,
        VariableCoercionHelper coercionHelper)
    {
        _next = next ??
            throw new ArgumentNullException(nameof(next));
        _contextPool = contextPool ??
            throw new ArgumentNullException(nameof(contextPool));
        _settings = options?.Complexity ??
            throw new ArgumentNullException(nameof(options));
        _cache = cache ??
            throw new ArgumentNullException(nameof(cache));
        _coercionHelper = coercionHelper ??
            throw new ArgumentNullException(nameof(coercionHelper));

        _compiler = new ComplexityAnalyzerCompilerVisitor(_settings);
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
                    var cacheId = context.CreateCacheId(context.OperationId);
                    var document = context.Document;
                    var operationDefinition =
                        context.Operation?.Definition ??
                        document.GetOperation(context.Request.OperationName);

                    if (!_cache.TryGetAnalyzer(cacheId, out var analyzer))
                    {
                        analyzer = CompileAnalyzer(context, document, operationDefinition, cacheId);
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
        string cacheId)
    {
        var validatorContext = _contextPool.Get();
        ComplexityAnalyzerDelegate? operationAnalyzer = null;

        try
        {
            PrepareContext(requestContext, document, validatorContext);

            _compiler.Visit(document, validatorContext);
            var analyzers = (List<OperationComplexityAnalyzer>)validatorContext.List.Peek()!;

            foreach (var analyzer in analyzers)
            {
                if (analyzer.OperationDefinitionNode.Equals(operationDefinition, SyntaxComparison.Syntax))
                {
                    operationAnalyzer = analyzer.Analyzer;

                    _cache.TryAddAnalyzer(
                        cacheId,
                        analyzer.Analyzer);
                }
            }

            return operationAnalyzer!;
        }
        finally
        {
            validatorContext.Clear();
            _contextPool.Return(validatorContext);
        }
    }

    private void PrepareContext(
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
}
