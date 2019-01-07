using System.Diagnostics;

namespace HotChocolate.Execution.Instrumentation
{
    internal static class QueryDiagnosticEvents
    {
        private const string _queryErrorEventName = "QueryError";
        private const string _validationErrorEventName = "ValidationError";

        private static readonly DiagnosticSource _src =
            new DiagnosticListener(Constants.DiagnosticListenerName);

        public static Activity BeginExecute(IQueryContext context)
        {
            var payload = new
            {
                schema = context.Schema,
                request = context.Request
            };

            if (_src.IsEnabled(Constants.QueryActivityName, payload))
            {
                var activity = new Activity(Constants.QueryActivityName);

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

                if (_src.IsEnabled(Constants.QueryActivityName, payload))
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

            if (_src.IsEnabled(_queryErrorEventName, payload))
            {
                _src.Write(_queryErrorEventName, payload);
            }
        }

        public static void ValidationError(IQueryContext context)
        {
            var payload = new
            {
                schema = context.Schema,
                request = context.Request,
                query = context.Document,
                errors = context.ValidationResult.Errors
            };

            if (_src.IsEnabled(_validationErrorEventName, payload))
            {
                _src.Write(_validationErrorEventName, payload);
            }
        }
    }
}
