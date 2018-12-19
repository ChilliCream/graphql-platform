using System;
using System.Collections.Generic;
using System.Diagnostics;
using HotChocolate.Language;

namespace HotChocolate.Execution
{
    internal static class QueryDiagnosticEvents
    {
        private const string _diagnosticListenerName = "HotChocolate.Execution";
        private const string _queryActivityName = "Query";
        private const string _queryErrorEventName = "QueryError";
        private const string _validationErrorEventName = "ValidationError";

        private static readonly DiagnosticSource _src =
            new DiagnosticListener(_diagnosticListenerName);

        public static Activity BeginExecute(
            ISchema schema,
            IReadOnlyQueryRequest request)
        {
            var payload = new
            {
                Schema = schema,
                Request = request
            };

            if (_src.IsEnabled(_queryActivityName, payload))
            {
                var activity = new Activity(_queryActivityName);

                _src.StartActivity(activity, payload);

                return activity;
            }

            return null;
        }

        public static void EndExecute(
            Activity activity,
            ISchema schema,
            IReadOnlyQueryRequest request,
            DocumentNode query)
        {
            if (activity != null)
            {
                var payload = new
                {
                    Schema = schema,
                    Request = request,
                    Query = query
                };

                if (_src.IsEnabled(_queryActivityName, payload))
                {
                    _src.StopActivity(activity, payload);
                }
            }
        }

        public static void QueryError(
            ISchema schema,
            IReadOnlyQueryRequest request,
            DocumentNode query,
            Exception exception)
        {
            var payload = new
            {
                Schema = schema,
                Request = request,
                Query = query,
                Exception = exception
            };

            if (_src.IsEnabled(_queryErrorEventName, payload))
            {
                _src.Write(_queryErrorEventName, payload);
            }
        }

        public static void ValidationError(
            ISchema schema,
            IReadOnlyQueryRequest request,
            DocumentNode query,
            IReadOnlyCollection<IError> errors)
        {
            var payload = new
            {
                Schema = schema,
                Request = request,
                Query = query,
                Errors = errors
            };

            if (_src.IsEnabled(_validationErrorEventName, payload))
            {
                _src.Write(_validationErrorEventName, payload);
            }
        }
    }
}
