using System.Diagnostics;

namespace HotChocolate.Execution.Instrumentation
{
    internal static class ValidationDiagnosticEvents
    {
        private static readonly DiagnosticSource _src =
            new DiagnosticListener(DiagnosticNames.Listener);

        public static Activity BeginValidation(
            IQueryContext context)
        {
            var payload = new
            {
                schema = context.Schema,
                request = context.Request,
                query = context.Document
            };

            if (_src.IsEnabled(DiagnosticNames.Validation, payload))
            {
                var activity = new Activity(DiagnosticNames.Validation);

                _src.StartActivity(activity, payload);

                return activity;
            }

            return null;
        }

        public static void EndValidation(
            Activity activity,
            IQueryContext context)
        {
            if (activity != null)
            {
                var payload = new
                {
                    schema = context.Schema,
                    request = context.Request,
                    query = context.Document,
                    result = context.ValidationResult
                };

                if (_src.IsEnabled(DiagnosticNames.Validation, payload))
                {
                    _src.StopActivity(activity, payload);
                }
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

            if (_src.IsEnabled(DiagnosticNames.ValidationError, payload))
            {
                _src.Write(DiagnosticNames.ValidationError, payload);
            }
        }
    }
}
