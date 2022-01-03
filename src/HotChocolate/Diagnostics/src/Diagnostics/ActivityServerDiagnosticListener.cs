namespace HotChocolate.Diagnostics;

public class ActivityServerDiagnosticListener : ServerDiagnosticEventListener
{
    private readonly ActivityEnricher _enricher;

    public ActivityServerDiagnosticListener(ActivityEnricher activityEnricher)
        => _enricher = activityEnricher;
}
