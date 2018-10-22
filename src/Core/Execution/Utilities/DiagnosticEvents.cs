using System;
using System.Diagnostics;
using HotChocolate.Resolvers;

namespace HotChocolate.Execution
{
    internal static class DiagnosticEvents
    {
        private const string _diagnosticListenerName = "HotChocolate.Execution";
        private const string _resolverActivityName = "HotChocolate.Execution.ResolveField";
        private const string _resolverActivityNameStart = _resolverActivityName + ".Start";
        private const string _exceptionEventName = "HotChocolate.Execution.ResolveField";

        private static readonly DiagnosticListener _diagnosticListener =
            new DiagnosticListener(_diagnosticListenerName);

        public static Activity BeginResolveField(
            IResolverContext resolverContext)
        {
            if (_diagnosticListener.IsEnabled(
                _resolverActivityName,
                resolverContext))
            {
                var activity = new Activity(_resolverActivityName);

                if (_diagnosticListener.IsEnabled(_resolverActivityNameStart))
                {
                    _diagnosticListener.StartActivity(activity, new
                    {
                        Context = resolverContext,
                        Timestamp = Stopwatch.GetTimestamp()
                    });
                }
                else
                {
                    activity.Start();
                }

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
                _diagnosticListener.StopActivity(activity, new
                {
                    Context = resolverContext,
                    Result = resolvedValue,
                    Timestamp = Stopwatch.GetTimestamp()
                });
            }
        }

        public static void ResolverError(
            IResolverContext resolverContext,
            Exception exception)
        {
            if (_diagnosticListener.IsEnabled(_exceptionEventName))
            {
                _diagnosticListener.Write(_exceptionEventName, new
                {
                    Context = resolverContext,
                    Exception = exception
                });
            }
        }
    }
}
