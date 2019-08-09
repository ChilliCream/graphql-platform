using System;
using System.Globalization;
using System.Threading.Tasks;
using HotChocolate.Execution.Configuration;
using HotChocolate.Properties;
using HotChocolate.Validation;

namespace HotChocolate.Execution
{
    internal sealed class MaxComplexityMiddleware
    {
        private static readonly MaxComplexityVisitor _visitor =
            new MaxComplexityVisitor();
        private readonly QueryDelegate _next;
        private readonly IValidateQueryOptionsAccessor _options;
        private readonly ComplexityCalculation _calculation;

        public MaxComplexityMiddleware(
            QueryDelegate next,
            IValidateQueryOptionsAccessor options,
            ComplexityCalculation complexityCalculation)
        {
            _next = next
                ?? throw new ArgumentNullException(nameof(next));
            _options = options
                ?? throw new ArgumentNullException(nameof(options));
            _calculation = complexityCalculation
                ?? Complexity.MultiplierCalculation;
        }

        public Task InvokeAsync(IQueryContext context)
        {
            if (_options.UseComplexityMultipliers == true
                && _options.MaxOperationComplexity.HasValue)
            {
                if (IsContextIncomplete(context))
                {
                    context.Result = QueryResult.CreateError(
                        ErrorBuilder.New()
                            .SetMessage(CoreResources.MaxComplexityMiddleware_Prerequisite)
                            .SetCode(MiddlewareErrorCodes.Incomplete)
                            .Build());
                    return Task.CompletedTask;
                }
                else
                {
                    var visitorContext = MaxComplexityVisitorContext.New(
                        context.Schema,
                        context.Operation.Variables,
                        _calculation);

                    int complexity = _visitor.Visit(
                        context.Document,
                        context.Operation.Definition,
                        visitorContext);

                    if (IsAllowedComplexity(complexity))
                    {
                        return _next(context);
                    }
                    else
                    {
                        IError error = ErrorBuilder.New()
                            .SetMessage(string.Format(
                                CultureInfo.InvariantCulture,
                                CoreResources
                                    .MaxComplexityMiddleware_NotAllowed,
                                complexity,
                                _options.MaxOperationComplexity))
                            .AddLocation(context.Operation.Definition)
                            .Build();

                        context.Result = QueryResult.CreateError(error);
                        return Task.CompletedTask;
                    }
                }
            }

            return _next(context);
        }

        private bool IsAllowedComplexity(int complexity)
        {
            return _options.MaxOperationComplexity.HasValue
                && _options.MaxOperationComplexity.Value >= complexity;
        }


        private static bool IsContextIncomplete(IQueryContext context)
        {
            return context.Document == null
                || context.Operation == null;
        }
    }
}
