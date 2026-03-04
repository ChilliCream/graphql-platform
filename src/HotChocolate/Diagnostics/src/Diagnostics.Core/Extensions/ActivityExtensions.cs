using System.Diagnostics;
using System.Runtime.CompilerServices;
using OpenTelemetry.Trace;
using Status = OpenTelemetry.Trace.Status;

namespace HotChocolate.Diagnostics;

internal static class ActivityExtensions
{
    extension(Activity activity)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void MarkAsSuccess()
        {
            activity.SetStatus(Status.Ok);
            activity.SetStatus(ActivityStatusCode.Ok);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void MarkAsError()
        {
            activity.SetStatus(Status.Error);
            activity.SetStatus(ActivityStatusCode.Error);
        }

        public void RecordError(IError error)
        {
            if (error.Exception is { } exception)
            {
                activity.RecordException(exception);
            }

            var tags = new ActivityTagsCollection
            {
                [SemanticConventions.GraphQL.Error.Message] = error.Message
            };

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

            // TODO: Not sure if this is correct according to the spec
            activity.AddEvent(new ActivityEvent("exception", default, tags));
        }
    }
}
