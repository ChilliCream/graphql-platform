using System;
using System.Diagnostics;
using HotChocolate.Resolvers;

namespace HotChocolate.Execution.Instrumentation
{
    internal static class ResolverDiagnosticEvents
    {
        private static readonly DiagnosticSource _src =
            new DiagnosticListener(DiagnosticNames.Listener);

        public static Activity BeginResolveField(
            IResolverContext resolverContext)
        {
            var payload = new
            {
                context = resolverContext
            };

            if (_src.IsEnabled(DiagnosticNames.Resolver, payload))
            {
                var activity = new Activity(DiagnosticNames.Resolver);

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

                if (_src.IsEnabled(DiagnosticNames.Resolver, payload))
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

            if (_src.IsEnabled(DiagnosticNames.ResolverError, payload))
            {
                _src.Write(DiagnosticNames.ResolverError, payload);
            }
        }
    }
}
