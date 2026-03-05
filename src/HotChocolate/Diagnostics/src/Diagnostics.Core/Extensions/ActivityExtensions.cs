using System.Diagnostics;
using System.Runtime.CompilerServices;
using HotChocolate.Execution;
using HotChocolate.Language;

namespace HotChocolate.Diagnostics;

internal static class ActivityExtensions
{
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
        }
#endif

        public void AddGraphQLError(IError error)
        {
            var tags = new ActivityTagsCollection
            {
                [SemanticConventions.GraphQL.Error.Message] = error.Message
            };

            if (error.Exception is { } exception)
            {
                tags["exception.message"] = exception.Message;
                tags["exception.stacktrace"] = exception.ToString();
                tags["exception.type"] = exception.GetType().ToString();
            }

            if (error.Path is not null)
            {
                tags[SemanticConventions.GraphQL.Error.Path] = error.Path.Print();
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
                        ["line"] = location.Line,
                        ["column"] = location.Column
                    };
                }

                tags[SemanticConventions.GraphQL.Error.Locations] = locations;
            }

            activity.AddEvent(new ActivityEvent("exception", default, tags));
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
