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

            QueryTypeName = options.QueryTypeName
                ?? "Query";
            MutationTypeName = options.MutationTypeName
                ?? "Mutation";
            SubscriptionTypeName = options.SubscriptionTypeName
                ?? "Subscription";
            ExecutionTimeout = options.ExecutionTimeout < MinExecutionTimeout
                    ? MinExecutionTimeout
                    : options.ExecutionTimeout;
            StrictValidation = options.StrictValidation;
            DeveloperMode = options.DeveloperMode;

            if (options.MaxExecutionDepth.HasValue
                && options.MaxExecutionDepth >= MinMaxExecutionDepth)
            {
                MaxExecutionDepth = options.MaxExecutionDepth;
            }
        }

        public string QueryTypeName { get; }

        public string MutationTypeName { get; }

        public string SubscriptionTypeName { get; }

        public int? MaxExecutionDepth { get; }

        public TimeSpan ExecutionTimeout { get; }

        public bool StrictValidation { get; }

        public bool DeveloperMode { get; }
    }
}
