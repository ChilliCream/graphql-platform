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

        public void AddGraphQLErrorEvent(
            IError error,
            string? operationType = null,
            string? operationName = null,
            string? schemaCoordinate = null)
        {
            var tags = new ActivityTagsCollection
            {
                [SemanticConventions.GraphQL.Error.Message] = error.Message
            };

            if (error.Path is not null)
            {
                tags[SemanticConventions.GraphQL.Field.Path] = error.Path.Print();
            }

            if (!string.IsNullOrEmpty(schemaCoordinate))
            {
                tags[SemanticConventions.GraphQL.Field.SchemaCoordinate] = schemaCoordinate;
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

        public void AddGraphQLErrorEvent(
            Exception exception,
            string? schemaCoordinate = null,
            string? operationType = null,
            string? operationName = null)
        {
            var tags = new ActivityTagsCollection
            {
                [SemanticConventions.GraphQL.Error.Message] = exception.Message,
                ["exception.type"] = exception.GetType().FullName,
                ["exception.message"] = exception.Message,
                ["exception.stacktrace"] = exception.ToString()
            };

            if (!string.IsNullOrEmpty(schemaCoordinate))
            {
                tags[SemanticConventions.GraphQL.Field.SchemaCoordinate] = schemaCoordinate;
            }

            if (!string.IsNullOrEmpty(operationType))
            {
                tags[SemanticConventions.GraphQL.Operation.Type] = operationType;
            }

            if (!string.IsNullOrEmpty(operationName))
            {
                tags[SemanticConventions.GraphQL.Operation.Name] = operationName;
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
        public void SetErrorType(string errorType)
        {
            if (activity.GetTagItem(SemanticConventions.ErrorType) is null)
            {
                activity.SetTag(SemanticConventions.ErrorType, errorType);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetErrorType(
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
