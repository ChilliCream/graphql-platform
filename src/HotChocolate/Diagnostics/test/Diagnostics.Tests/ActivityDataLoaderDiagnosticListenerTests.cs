using System.Text;
using Microsoft.Extensions.ObjectPool;
using static HotChocolate.Diagnostics.ActivityTestHelper;

namespace HotChocolate.Diagnostics;

[Collection("Instrumentation")]
public class ActivityDataLoaderDiagnosticListenerTests
{
    [Fact]
    public void Run_Batch_Dispatch_Coordinator_Emits_Activity()
    {
        using (CaptureActivities(out var activities))
        {
            var listener = CreateListener(ActivityScopes.DataLoaderBatch);

            using (listener.RunBatchDispatchCoordinator())
            {
            }

            activities.MatchSnapshot();
        }
    }

    [Fact]
    public void Run_Batch_Dispatch_Coordinator_Tracks_Dispatch_Events()
    {
        using (CaptureActivities(out var activities))
        {
            var listener = CreateListener(ActivityScopes.DataLoaderBatch);

            using (listener.RunBatchDispatchCoordinator())
            {
                listener.BatchEvaluated(2);
                listener.BatchDispatched(1);
            }

            activities.MatchSnapshot();
        }
    }

    private static Listeners.ActivityDataLoaderDiagnosticListener CreateListener(ActivityScopes scopes)
    {
        var options = new InstrumentationOptions
        {
            Scopes = scopes
        };
        var pool = new DefaultObjectPoolProvider().Create(new StringBuilderPooledObjectPolicy());
        var enricher = new ActivityEnricher(pool, options);
        return new Listeners.ActivityDataLoaderDiagnosticListener(enricher, options);
    }
}
