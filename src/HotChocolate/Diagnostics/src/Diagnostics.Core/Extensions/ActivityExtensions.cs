using System.Diagnostics;
using System.Runtime.CompilerServices;
using HotChocolate.Execution;
using HotChocolate.Language;

namespace HotChocolate.Diagnostics;

internal static class ActivityExtensions
{
    /// <summary>
    /// The <c>error.type</c> value used as a fallback when an error occurs during
    /// the GraphQL operation execution phase.
    /// </summary>
    public const string ExecutionErrorType = "EXECUTION_ERROR";

    /// <summary>
    /// The <c>error.type</c> value used as a fallback when an error occurs during
    /// the GraphQL document validation phase.
    /// </summary>
    public const string ValidationErrorType = "GRAPHQL_VALIDATION_FAILED";

    /// <summary>
    /// The <c>error.type</c> value used as a fallback when an error occurs during
    /// the GraphQL document parsing phase.
    /// </summary>
    public const string ParseErrorType = "GRAPHQL_PARSE_FAILED";

    extension(Activity activity)
    {
#if !NET9_0_OR_GREATER
        public void AddException(Exception exception)
        {
            activity.AddEvent(
                new ActivityEvent(
                    "exception",
                    tags: new ActivityTagsCollection
                    {
                        { "exception.message", exception.Message },
                        { "exception.stacktrace", exception.ToString() },
                        { "exception.type", exception.GetType().ToString() }
                    }));

            activity.SetErrorType(exception);
        }
#endif

        /// <summary>
        /// Sets the <c>error.type</c> tag for a GraphQL error on the activity.
        /// By default prefers <see cref="IError.Code"/> (sourced from
        /// <c>extensions.code</c>), then the underlying exception type name, and
        /// finally falls back to the supplied <paramref name="fallback"/>.
        /// When <paramref name="preferException"/> is <see langword="true"/>, the
        /// exception type name is preferred over the error code, matching the
        /// guidance for the field execution span.
        /// </summary>
        public void SetGraphQLErrorType(
            IError error,
            string fallback,
            bool preferException = false)
        {
            if (activity.GetTagItem(SemanticConventions.ErrorType) is not null)
            {
                return;
            }

            var exceptionType = error.Exception?.GetType().FullName;

            string? errorType;
            if (preferException)
            {
                errorType = exceptionType
                    ?? (!string.IsNullOrEmpty(error.Code) ? error.Code : fallback);
            }
            else
            {
                errorType = !string.IsNullOrEmpty(error.Code)
                    ? error.Code
                    : exceptionType ?? fallback;
            }

            activity.SetTag(SemanticConventions.ErrorType, errorType);
        }

        /// <summary>
        /// Adds a <c>graphql.error</c> event to the activity following the
        /// OpenTelemetry GraphQL semantic conventions. When the GraphQL error
        /// carries an underlying exception, <c>exception.type</c>,
        /// <c>exception.message</c>, and <c>exception.stacktrace</c> are added
        /// to the event. When no exception is present, those attributes are
        /// omitted (the GraphQL error message is available via
        /// <c>graphql.error.message</c>).
        /// </summary>
        public void AddGraphQLErrorEvent(
            IError error,
            string? operationType = null,
            string? operationName = null)
        {
            var tags = new ActivityTagsCollection
            {
                [SemanticConventions.GraphQL.Error.Message] = error.Message
            };

            // TODO: Fusion's source-schema error remapping currently loses the
            // field path on errors that originate from nested source-schema
            // resolvers. The IError reaches us without `.Path` set, so the
            // emitted `graphql.error` event omits `graphql.field.path` even
            // though the spec marks it as conditionally required when the
            // error is associated with a field. Tracking issue:
            // https://github.com/ChilliCream/graphql-platform/issues/9624
            if (error.Path is not null)
            {
                tags[SemanticConventions.GraphQL.Field.Path] = error.Path.Print();
            }

            if (!string.IsNullOrEmpty(error.Code))
            {
                tags[SemanticConventions.GraphQL.Error.Code] = error.Code;
            }

            if (error.Locations is { Count: > 0 })
            {
                var locations = new object[error.Locations.Count];
                for (var i = 0; i < error.Locations.Count; i++)
                {
                    var location = error.Locations[i];
                    locations[i] = new Dictionary<string, int>
                    {
                        [SemanticConventions.GraphQL.Document.Location.Line] = location.Line,
                        [SemanticConventions.GraphQL.Document.Location.Column] = location.Column
                    };
                }

                tags[SemanticConventions.GraphQL.Document.Locations] = locations;
            }

            if (operationType is not null)
            {
                tags[SemanticConventions.GraphQL.Operation.Type] = operationType;
            }

            if (!string.IsNullOrEmpty(operationName))
            {
                tags[SemanticConventions.GraphQL.Operation.Name] = operationName;
            }

            if (error.Exception is { } exception)
            {
                tags["exception.type"] = exception.GetType().FullName;
                tags["exception.message"] = exception.Message;
                tags["exception.stacktrace"] = exception.ToString();
            }

            activity.AddEvent(new ActivityEvent("graphql.error", default, tags));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetErrorType(Exception exception)
        {
            if (activity.GetTagItem(SemanticConventions.ErrorType) is null)
            {
                activity.SetTag(SemanticConventions.ErrorType, exception.GetType().FullName);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EnrichDocumentInfo(OperationDocumentInfo documentInfo)
        {
            var hash = documentInfo.Hash;

            if (!hash.IsEmpty)
            {
                activity.SetTag(
                    SemanticConventions.GraphQL.Document.Hash,
                    $"{hash.AlgorithmName}:{hash.Value}");
            }

            if (documentInfo is { IsPersisted: true, Id.HasValue: true })
            {
                activity.SetTag(
                    SemanticConventions.GraphQL.Document.Id,
                    documentInfo.Id.Value);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EnrichOperation(OperationType operationType, string? operationName)
        {
            activity.SetTag(
                SemanticConventions.GraphQL.Operation.Type,
                SemanticConventions.GraphQL.Operation.TypeValues[operationType]);

            if (!string.IsNullOrEmpty(operationName))
            {
                activity.SetTag(
                    SemanticConventions.GraphQL.Operation.Name,
                    operationName);
            }
        }
    }
}
