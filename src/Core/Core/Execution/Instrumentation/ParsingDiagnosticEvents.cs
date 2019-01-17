using System.Diagnostics;

namespace HotChocolate.Execution.Instrumentation
{
    internal static class ParsingDiagnosticEvents
    {
        private static readonly DiagnosticSource _src =
            new DiagnosticListener(DiagnosticNames.Listener);

        public static Activity BeginParsing(
            IQueryContext context)
        {
            var payload = new
            {
                schema = context.Schema,
                request = context.Request,
                query = context.Document
            };

            if (_src.IsEnabled(DiagnosticNames.Parsing, payload))
            {
                var activity = new Activity(DiagnosticNames.Parsing);

                _src.StartActivity(activity, payload);

                return activity;
            }

            return null;
        }

        public static void EndParsing(
            Activity activity,
            IQueryContext context)
        {
            if (activity != null)
            {
                var payload = new
                {
                    schema = context.Schema,
                    request = context.Request,
                    query = context.Document
                };

                if (_src.IsEnabled(DiagnosticNames.Parsing, payload))
                {
                    _src.StopActivity(activity, payload);
                }
            }
        }
    }
}
