using System.Diagnostics;

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
            if (error.Exception is { } exception)
            {
                activity.AddException(exception);
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
