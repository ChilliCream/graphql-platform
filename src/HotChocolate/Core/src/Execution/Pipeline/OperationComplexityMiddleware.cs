using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Execution.Caching;
using HotChocolate.Execution.Options;
using HotChocolate.Execution.Pipeline.Complexity;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.Validation;
using static HotChocolate.WellKnownContextData;
using static HotChocolate.Execution.ErrorHelper;
using static HotChocolate.Execution.Pipeline.PipelineTools;

namespace HotChocolate.Execution.Pipeline
{
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
            if (_settings.Enable && !context.ContextData.ContainsKey(NoComplexityAnalysis))
            {
                if (context.DocumentId is not null &&
                    context.OperationId is not null &&
                    context.Document is not null)
                {
                    string cacheId = context.CreateCacheId(context.OperationId);
                    DocumentNode document = context.Document;
                    OperationDefinitionNode operationDefinition =
                        context.Operation?.Definition ??
                        document.GetOperation(context.Request.OperationName);

                    if (!_cache.TryGetOperation(cacheId, out ComplexityAnalyzerDelegate? analyzer))
                    {
                        analyzer = CompileAnalyzer(context, document, operationDefinition);
                    }

                    CoerceVariables(
                        context,
                        _coercionHelper,
                        operationDefinition.VariableDefinitions);

                    var complexity = analyzer(context.Variables!);
                    context.ContextData[_settings.ContextDataKey] = complexity;

                    if (complexity <= _settings.MaximumAllowed)
                    {
                        await _next(context).ConfigureAwait(false);
                    }
                    else
                    {
                        context.Result = MaxComplexityReached(complexity, _settings.MaximumAllowed);
                    }
                }
                else
                {
                    context.Result = StateInvalidForComplexityAnalyzer();
                }
            }
            else
            {
                await _next(context).ConfigureAwait(false);
            }
        }

        private ComplexityAnalyzerDelegate CompileAnalyzer(
            IRequestContext requestContext,
            DocumentNode document,
            OperationDefinitionNode operationDefinition)
        {
            DocumentValidatorContext validatorContext = _contextPool.Get();
            ComplexityAnalyzerDelegate? operationAnalyzer = null;

            try
            {
                PrepareContext(requestContext, document, validatorContext);

                _compiler.Visit(document, validatorContext);
                var analyzers = (List<OperationComplexityAnalyzer>)validatorContext.List.Peek()!;

                foreach (var analyzer in analyzers)
                {
                    if (analyzer.OperationDefinitionNode == operationDefinition)
                    {
                        operationAnalyzer = analyzer.Analyzer;
                    }

                    _cache.TryAddOperation(
                        requestContext.CreateCacheId(
                            CreateOperationId(
                                requestContext.DocumentId!,
                                analyzer.OperationDefinitionNode.Name?.Value)),
                        analyzer.Analyzer);
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
    }
}
