
namespace Prometheus.Abstractions
{
    public sealed class FloatValue
        : ScalarValue<decimal>
    {
        public FloatValue(decimal value)
            : base(value, NamedType.Float)
        {
        }
    }
}