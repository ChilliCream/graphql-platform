using System.Diagnostics;
using System.Runtime.CompilerServices;
using HotChocolate.Execution;
using HotChocolate.Language;

namespace HotChocolate.Diagnostics;

internal static class ActivityExtensions
{
    /// <summary>
    /// The default <c>error.type</c> value used when a GraphQL error has no
    /// <c>extensions.code</c> and is not associated with an exception.
    /// </summary>
    public const string DefaultErrorType = "GRAPHQL_ERROR";

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
        /// Prefers <see cref="IError.Code"/> (sourced from <c>extensions.code</c>),
        /// then the underlying exception type name, and finally falls back to the
        /// supplied <paramref name="fallback"/>.
        /// </summary>
        public void SetGraphQLErrorType(IError error, string fallback = DefaultErrorType)
        {
            if (activity.GetTagItem(SemanticConventions.ErrorType) is not null)
            {
                return;
            }

            var errorType = !string.IsNullOrEmpty(error.Code)
                ? error.Code
                : error.Exception?.GetType().FullName ?? fallback;
            activity.SetTag(SemanticConventions.ErrorType, errorType);
        }

        /// <summary>
        /// Adds a <c>graphql.error</c> event to the activity following the
        /// OpenTelemetry GraphQL semantic conventions. The event always carries
        /// <c>exception.type</c>, <c>exception.message</c>, and
        /// <c>exception.stacktrace</c> derived from the underlying exception when
        /// available, or from the GraphQL error itself otherwise.
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

            if (error.Extensions is not null
                && error.Extensions.TryGetValue("schemaCoordinate", out var schemaCoordinate)
                && schemaCoordinate is string schemaCoordinateString
                && !string.IsNullOrEmpty(schemaCoordinateString))
            {
                tags[SemanticConventions.GraphQL.Field.SchemaCoordinate] = schemaCoordinateString;
            }

            if (operationType is not null)
            {
                tags[SemanticConventions.GraphQL.Operation.Type] = operationType;
            }

            if (!string.IsNullOrEmpty(operationName))
            {
                tags[SemanticConventions.GraphQL.Operation.Name] = operationName;
            }

            tags["exception.type"] = error.Exception?.GetType().FullName ?? DefaultErrorType;
            tags["exception.message"] = error.Exception?.Message ?? error.Message;
            tags["exception.stacktrace"] = error.Exception?.ToString() ?? string.Empty;

            activity.AddEvent(new ActivityEvent("graphql.error", default, tags));
        }

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
