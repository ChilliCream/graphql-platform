using System;
using System.Collections.Generic;
using System.Diagnostics;
using HotChocolate.Language;
using HotChocolate.Resolvers;

namespace HotChocolate.Execution
{
    internal static class QueryDiagnosticEvents
    {
        private const string _diagnosticListenerName = "HotChocolate.Execution";
        private const string _queryActivityName =
            _diagnosticListenerName + ".Query";
        private const string _queryActivityStartName =
            _queryActivityName + ".Start";
        private const string _queryActivityStopName =
            _queryActivityName + ".Stop";
        private const string _queryErrorEventName =
            _queryActivityName + ".QueryError";

        private static readonly DiagnosticSource _src =
            new DiagnosticListener(_diagnosticListenerName);

        public static Activity BeginExecute(
            ISchema schema,
            QueryRequest request)
        {
            if (_src.IsEnabled(_queryActivityStartName, schema, request)
                || _src.IsEnabled(_queryActivityStopName, schema, request))
            {
                var activity = new Activity(_queryActivityName);

                _src.StartActivity(activity, new
                {
                    Schema = schema,
                    Request = request,
                    Timestamp = Stopwatch.GetTimestamp()
                });

                return activity;
            }

            return null;
        }

        public static void EndExecute(
            Activity activity,
            ISchema schema,
            QueryRequest request,
            DocumentNode query)
        {
            if (activity != null)
            {
                _src.StopActivity(activity, new
                {
                    Schema = schema,
                    Request = request,
                    Query = query,
                    Timestamp = Stopwatch.GetTimestamp()
                });
            }
        }

        public static void QueryError(
            ISchema schema,
            QueryRequest request,
            DocumentNode query,
            Exception exception)
        {
            if (_src.IsEnabled(_queryErrorEventName))
            {
                _src.Write(_queryErrorEventName, new
                {
                    Schema = schema,
                    Request = request,
                    Query = query,
                    Exception = exception
                });
            }
        }

        public static void ValidationError(
            ISchema schema,
            QueryRequest request,
            DocumentNode query,
            IReadOnlyCollection<IQueryError> errors)
        {
            if (_src.IsEnabled(_queryErrorEventName))
            {
                _src.Write(_queryErrorEventName, new
                {
                    Schema = schema,
                    Request = request,
                    Query = query,
                    Errors = errors
                });
            }
        }
    }
}
