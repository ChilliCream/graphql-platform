
namespace Prometheus.Abstractions
{
    public interface IScalarValue
        : IValue
    {
        NamedType Type { get; }
    }
}