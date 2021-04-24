using System;
using System.Diagnostics;
using HotChocolate.Execution.Pipeline.Complexity;
using HotChocolate.Types;
using HotChocolate.Validation.Options;

namespace HotChocolate.Execution.Options
{
    /// <summary>
    /// Represents the entirety of settings to configure the behavior of the
    /// query execution engine.
    /// </summary>
    public class RequestExecutorOptions : IRequestExecutorOptionsAccessor
    {
        private const int _minQueryCacheSize = 10;
        private static readonly TimeSpan _minExecutionTimeout =
            TimeSpan.FromMilliseconds(100);

        private TimeSpan _executionTimeout = TimeSpan.FromSeconds(30);
        private int _queryCacheSize = 100;

        /// <summary>
        /// Gets or sets maximum allowed execution time of a query. The default
        /// value is <c>30</c> seconds. The minimum allowed value is <c>100</c>
        /// milliseconds.
        /// </summary>
        public TimeSpan ExecutionTimeout
        {
            get => _executionTimeout;
            set
            {
                _executionTimeout = (value < _minExecutionTimeout)
                    ? _minExecutionTimeout
                    : value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the <c>GraphQL</c> errors
        /// should be extended with exception details. The default value is
        /// <see cref="Debugger.IsAttached"/>.
        /// </summary>
        public bool IncludeExceptionDetails { get; set; } =
            Debugger.IsAttached;

        /// <summary>
        /// Gets or sets the maximum amount of compiled queries that can be cached. The
        /// default value is <c>100</c>. The minimum allowed value is
        /// <c>10</c>.
        /// </summary>
        [Obsolete(
            "Use AddDocumentCache or AddOperationCache on the IServiceCollection.",
            true)]
        public int QueryCacheSize
        {
            get => _queryCacheSize;
            set
            {
                _queryCacheSize = (value < _minQueryCacheSize)
                    ? _minQueryCacheSize
                    : value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether tracing for performance
        /// measurement of query requests is enabled. The default value is
        /// <see cref="TracingPreference.Never"/>.
        /// </summary>
        [Obsolete("Use AddApolloTracing on the IRequestExecutorBuilder.", true)]
        public TracingPreference TracingPreference { get; set; }

        [Obsolete("This can now be configured on the validation rule.", true)]
        public bool? UseComplexityMultipliers { get; set; }
    }


        public interface IMaxComplexityOptionsAccessor
        {
            MaxComplexitySettings Complexity { get; }
        }

    public class MaxComplexitySettings
    {
        public int? MaximumAllowed { get; set; }

        public bool ApplyDefaults { get; set; }

        public int DefaultComplexity { get; set; }

        public int DefaultResolverComplexity { get; set; }

        public string ContextDataKey { get; set; }

        private ComplexityCalculation ComplexityCalculation { get; set; }

        public static int DefaultCalculation(ComplexityContext context)
        {
            if (context.Multipliers.Count == 0)
            {
                return context.Complexity + context.ChildComplexity;
            }

            var cost = context.Complexity;
            var childCost = context.ChildComplexity;

            foreach (MultiplierPathString multiplier in context.Multipliers)
            {
                if (context.TryGetArgumentValue(multiplier, out int value))
                {
                    cost *= value;
                    childCost *= value;
                }
            }

            return cost + childCost;
        }
    }
}
