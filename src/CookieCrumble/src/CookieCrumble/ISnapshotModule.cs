using CookieCrumble.Formatters;

namespace CookieCrumble;

public interface ISnapshotModule
{
    void Initialize();
}

public abstract class SnapshotModule : ISnapshotModule
{
    public void Initialize()
    {
        var framework = TryCreateTestFramework();
        if (framework is not null)
        {
            Snapshot.RegisterTestFramework(framework);
        }

        foreach (var formatter in CreateFormatters())
        {
            Snapshot.RegisterFormatter(formatter);
        }
    }

    protected virtual ITestFramework? TryCreateTestFramework() => null;

    protected virtual IEnumerable<ISnapshotValueFormatter> CreateFormatters()
    {
        yield break;
    }
}
