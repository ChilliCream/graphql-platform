using System;

namespace HotChocolate.Configuration
{
    public class ReadOnlySchemaOptions
        : IReadOnlySchemaOptions
    {
        internal const int MinMaxExecutionDepth = 1;
        internal static readonly TimeSpan MinExecutionTimeout =
            TimeSpan.FromMilliseconds(100);

        public ReadOnlySchemaOptions(IReadOnlySchemaOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            QueryTypeName = options.QueryTypeName ?? "Query";
            MutationTypeName = options.MutationTypeName ?? "Mutation";
            SubscriptionTypeName = options.SubscriptionTypeName ?? "Subscription";
            ExecutionTimeout = options.ExecutionTimeout < MinExecutionTimeout
                    ? MinExecutionTimeout
                    : options.ExecutionTimeout;
            MaxExecutionDepth = options.MaxExecutionDepth < MinMaxExecutionDepth
                ? MinMaxExecutionDepth
                : options.MaxExecutionDepth;
        }

        public string QueryTypeName { get; }

        public string MutationTypeName { get; }

        public string SubscriptionTypeName { get; }

        public int MaxExecutionDepth { get; }

        public TimeSpan ExecutionTimeout { get; }
    }
}
