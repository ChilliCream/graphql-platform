
namespace Prometheus.Abstractions
{
    public sealed class StringValue
       : ScalarValue<string>
    {
        public StringValue(string value)
            : base(value, NamedType.String)
        {
        }
    }
}