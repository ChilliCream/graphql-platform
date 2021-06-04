using System;

namespace HotChocolate.Execution
{
    /// <summary>
    /// Extensions methods for <see cref="IQueryRequestBuilder"/>.
    /// </summary>
    public static class QueryRequestBuilderExtensions
    {
        /// <summary>
        /// Allows introspection usage in the current request.
        /// </summary>
        public static IQueryRequestBuilder AllowIntrospection(
            this IQueryRequestBuilder builder) =>
            builder.SetProperty(WellKnownContextData.IntrospectionAllowed, null);

        /// <summary>
        /// Sets the error message for when the introspection is not allowed.
        /// </summary>
        public static IQueryRequestBuilder SetIntrospectionNotAllowedMessage(
            this IQueryRequestBuilder builder,
            string message)
        {
            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            return builder.SetProperty(WellKnownContextData.IntrospectionMessage, message);
        }

        /// <summary>
        /// Sets the error message for when the introspection is not allowed.
        /// </summary>
        public static IQueryRequestBuilder SetIntrospectionNotAllowedMessage(
            this IQueryRequestBuilder builder,
            Func<string> messageFactory)
        {
            if (messageFactory is null)
            {
                throw new ArgumentNullException(nameof(messageFactory));
            }

            return builder.SetProperty(WellKnownContextData.IntrospectionMessage, messageFactory);
        }

        /// <summary>
        /// Skips the operation complexity analysis of this request.
        /// </summary>
        public static IQueryRequestBuilder SkipComplexityAnalysis(
            this IQueryRequestBuilder builder) =>
            builder.SetProperty(WellKnownContextData.SkipComplexityAnalysis, null);

        /// <summary>
        /// Set allowed complexity for this request and override the global allowed complexity.
        /// </summary>
        public static IQueryRequestBuilder SetMaximumAllowedComplexity(
            this IQueryRequestBuilder builder,
            int maximumAllowedComplexity) =>
            builder.SetProperty(
                WellKnownContextData.MaximumAllowedComplexity,
                maximumAllowedComplexity);
    }
}
