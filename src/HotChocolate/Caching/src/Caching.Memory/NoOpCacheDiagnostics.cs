namespace HotChocolate.Caching.Memory;

internal sealed class NoOpCacheDiagnostics : CacheDiagnostics
{
    public override void RegisterSizeGauge(Func<long> sizeProvider) { }
    public override void RegisterCapacityGauge(Func<long> sizeProvider) { }
    public override void Hit() { }
    public override void Miss() { }
    public override void Evict() { }

    public static NoOpCacheDiagnostics Instance { get; } = new();
}
