using System.Diagnostics;

namespace HotChocolate.Execution.Instrumentation
{
    internal static class QueryDiagnosticEvents
    {
        private static readonly DiagnosticSource _src =
            new DiagnosticListener(DiagnosticNames.Listener);

        public static Activity BeginExecute(IQueryContext context)
        {
            var payload = new
            {
                schema = context.Schema,
                request = context.Request,
                query = context.Document
            };

            if (_src.IsEnabled(DiagnosticNames.Query, payload))
            {
                var activity = new Activity(DiagnosticNames.Query);

                _src.StartActivity(activity, payload);

                return activity;
            }

            return null;
        }

        public static void EndExecute(Activity activity, IQueryContext context)
        {
            if (activity != null)
            {
                var payload = new
                {
                    schema = context.Schema,
                    request = context.Request,
                    query = context.Document,
                    result = context.Result
                };

                if (_src.IsEnabled(DiagnosticNames.Query, payload))
                {
                    _src.StopActivity(activity, payload);
                }
            }
        }

        public static void QueryError(IQueryContext context)
        {
            var payload = new
            {
                schema = context.Schema,
                request = context.Request,
                query = context.Document,
                exception = context.Exception
            };

            if (_src.IsEnabled(DiagnosticNames.QueryError, payload))
            {
                _src.Write(DiagnosticNames.QueryError, payload);
            }
        }
    }
}
