using System;
using System.Diagnostics;
using HotChocolate.Resolvers;

namespace HotChocolate.Execution
{
    internal static class ResolverDiagnosticEvents
    {
        private const string _diagnosticListenerName = "HotChocolate.Execution";
        private const string _resolverActivityName =
            "HotChocolate.Execution.ResolveField";
        private const string _resolverActivityStartName =
            _resolverActivityName + ".Start";
        private const string _resolverActivityStopName =
            _resolverActivityName + ".Stop";
        private const string _exceptionEventName =
            _resolverActivityName + ".Error";

        private static readonly DiagnosticSource _src =
            new DiagnosticListener(_diagnosticListenerName);

        public static Activity BeginResolveField(
            IResolverContext resolverContext)
        {
            if (_src.IsEnabled(_resolverActivityStartName, resolverContext)
                || _src.IsEnabled(_resolverActivityStopName, resolverContext))
            {
                var activity = new Activity(_resolverActivityName);

                _src.StartActivity(activity, new
                {
                    Context = resolverContext,
                    Timestamp = Stopwatch.GetTimestamp()
                });

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
                _src.StopActivity(activity, new
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
            if (_src.IsEnabled(_exceptionEventName))
            {
                _src.Write(_exceptionEventName, new
                {
                    Context = resolverContext,
                    Exception = exception
                });
            }
        }
    }
}
