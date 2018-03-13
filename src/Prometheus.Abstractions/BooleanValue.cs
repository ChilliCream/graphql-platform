
namespace Prometheus.Abstractions
{
    public class BooleanValue
        : ScalarValue<bool>
    {
        public BooleanValue(bool value)
            : base(value, NamedType.Boolean)
        {
        }
    }
}