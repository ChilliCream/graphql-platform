using RabbitMQ.Client;

namespace Mocha.Transport.RabbitMQ;

internal static class DateTimeExtensions
{
    extension(DateTimeOffset value)
    {
        public AmqpTimestamp ToAmqpTimestamp() => new(value.ToUnixTimeSeconds());
    }

    extension(DateTime value)
    {
        public AmqpTimestamp ToAmqpTimestamp()
        {
            var utc = value.Kind == DateTimeKind.Local ? value.ToUniversalTime() : value;

            return new AmqpTimestamp(new DateTimeOffset(utc, TimeSpan.Zero).ToUnixTimeSeconds());
        }
    }
}
