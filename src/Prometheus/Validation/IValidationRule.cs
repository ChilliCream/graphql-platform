namespace Prometheus.Validation
{
    public interface IValidationRule
    {
        string Code { get; }
        string Description { get; }
    }
}