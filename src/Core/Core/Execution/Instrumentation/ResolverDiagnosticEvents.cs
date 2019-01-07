using System;
using System.Diagnostics;
using HotChocolate.Resolvers;

namespace HotChocolate.Execution.Instrumentation
{
    internal static class ResolverDiagnosticEvents
    {
        private const string _exceptionEventName = "ResolverError";

        private static readonly DiagnosticSource _src =
            new DiagnosticListener(Constants.DiagnosticListenerName);

        public static Activity BeginResolveField(
            IResolverContext resolverContext)
        {
            var payload = new
            {
                context = resolverContext
            };

            if (_src.IsEnabled(Constants.ResolverActivityName, payload))
            {
                var activity = new Activity(Constants.ResolverActivityName);

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
                    context = resolverContext,
                    result = resolvedValue
                };

                if (_src.IsEnabled(Constants.ResolverActivityName, payload))
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
                context = resolverContext,
                exception
            };

            if (_src.IsEnabled(_exceptionEventName, payload))
            {
                _src.Write(_exceptionEventName, payload);
            }
        }
    }
}
