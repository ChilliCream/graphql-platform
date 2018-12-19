using System;
using System.Diagnostics;
using HotChocolate.Resolvers;

namespace HotChocolate.Execution
{
    internal static class ResolverDiagnosticEvents
    {
        private const string _diagnosticListenerName = "HotChocolate.Execution";
        private const string _resolverActivityName = "Resolver";
        private const string _exceptionEventName = "ResolverError";

        private static readonly DiagnosticSource _src =
            new DiagnosticListener(_diagnosticListenerName);

        public static Activity BeginResolveField(
            IResolverContext resolverContext)
        {
            var payload = new
            {
                Context = resolverContext
            };

            if (_src.IsEnabled(_resolverActivityName, payload))
            {
                var activity = new Activity(_resolverActivityName);

                _src.StartActivity(activity, payload);

                return activity;
            }

            return null;
        }

        public static void EndResolveField(
            Activity activity,
            IResolverContext resolverContext,
            object resolvedValue)
        {
            if (activity != null)
            {
                var payload = new
                {
                    Context = resolverContext,
                    Result = resolvedValue
                };

                if (_src.IsEnabled(_resolverActivityName, payload))
                {
                    _src.StopActivity(activity, payload);
                }
            }
        }

        public static void ResolverError(
            IResolverContext resolverContext,
            Exception exception)
        {
            var payload = new
            {
                Context = resolverContext,
                Exception = exception
            };

            if (_src.IsEnabled(_exceptionEventName, payload))
            {
                _src.Write(_exceptionEventName, payload);
            }
        }
    }
}
