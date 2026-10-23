namespace Mocha.Hosting;

internal sealed class MessageBusTransportsHealthCheckOptions
{
    public static readonly TimeSpan DefaultUnhealthyAfter = TimeSpan.FromMinutes(2);

    public TimeSpan UnhealthyAfter { get; set; } = DefaultUnhealthyAfter;
}
