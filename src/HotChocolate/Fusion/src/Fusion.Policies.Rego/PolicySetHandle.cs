using ChilliCream.Regorus;

namespace HotChocolate.Fusion.Policies.Rego;

/// <summary>
/// Owns the compiled policy set shared by the policies in one published snapshot.
/// </summary>
internal sealed class PolicySetHandle(CompiledPolicySet policySet) : IDisposable
{
    private CompiledPolicySet? _policySet = policySet;

    public CompiledPolicySet PolicySet
        => _policySet ?? throw new ObjectDisposedException(nameof(PolicySetHandle));

    public void Dispose() => Interlocked.Exchange(ref _policySet, null)?.Dispose();
}
