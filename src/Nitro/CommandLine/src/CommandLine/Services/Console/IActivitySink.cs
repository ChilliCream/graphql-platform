using Spectre.Console.Rendering;

namespace ChilliCream.Nitro.CommandLine;

internal interface IActivitySink
{
    Task Completion { get; }

    ActivityEntry AddRoot(string text);

    ActivityEntry AddChild(ActivityEntry parent, string text, ActivityState state);

    ActivityEntry CompleteChild(ActivityEntry parent, string text, ActivityState state);

    void SetState(ActivityEntry entry, ActivityState state);

    void SetTextAndState(ActivityEntry entry, string text, ActivityState state);

    void SetDetails(ActivityEntry entry, IRenderable details);

    void FailActiveDescendants(ActivityEntry entry);

    void Fail(ActivityEntry entry, string failureMessage);

    void Stop();
}
