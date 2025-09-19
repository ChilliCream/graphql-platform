namespace StrawberryShake;

public partial class CachePolicy : IDisposable
{
    private readonly IDisposable _session;

    public CachePolicy(IDisposable session)
    {
        _session = session;
    }

    public void Dispose()
    {
        _session.Dispose();
    }
}
