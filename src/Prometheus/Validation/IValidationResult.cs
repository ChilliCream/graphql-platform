namespace Prometheus.Validation
{
    public interface IValidationResult
    {
        IValidationRule Rule { get; }
    }
}