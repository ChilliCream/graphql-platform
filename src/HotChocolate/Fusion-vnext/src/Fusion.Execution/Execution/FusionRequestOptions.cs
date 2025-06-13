namespace HotChocolate.Fusion.Execution;

public sealed class FusionRequestOptions : ICloneable
{
    private static readonly TimeSpan s_minExecutionTimeout = TimeSpan.FromMilliseconds(100);
    private TimeSpan _executionTimeout;
    private bool _isReadOnly;

    public TimeSpan ExecutionTimeout
    {
        get => _executionTimeout;
        set
        {
            if (_isReadOnly)
            {
                throw new InvalidOperationException("The request options are read-only.");
            }

            _executionTimeout = value < s_minExecutionTimeout
                ? s_minExecutionTimeout
                : value;
        }
    }

    public FusionRequestOptions Clone()
    {
        var clone = new FusionRequestOptions();
        clone._executionTimeout = _executionTimeout;
        return clone;
    }

    object ICloneable.Clone() => Clone();

    internal void MakeReadOnly()
        => _isReadOnly = tr => Clone();
