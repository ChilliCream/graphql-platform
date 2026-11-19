using RabbitMQ.Client;

namespace Mocha.Transport.RabbitMQ;

internal static class RabbitMQChannelExtensions
{
    private static readonly CancellationToken s_cancelled = new(canceled: true);

    extension(IChannel channel)
    {
        /// <summary>
        /// Closes and disposes the channel, ignoring failures of either step. Completes when the client has
        /// given the channel number back, which takes as long as the client's timeout for a request when the
        /// broker does not answer.
        /// </summary>
        public async Task CloseCompletelyAsync()
        {
            try
            {
                if (channel.IsOpen)
                {
                    await channel.CloseAsync();
                }
                else
                {
                    // A connection with automatic recovery forgets a channel the broker closed only when the
                    // channel is closed here as well. The cancelled token keeps the client from waiting for a
                    // reply.
                    await channel.CloseAsync(Constants.ReplySuccess, "Goodbye", abort: true, s_cancelled);
                }
            }
            catch
            {
                // Ignore close errors.
            }
            finally
            {
                try
                {
                    await channel.DisposeAsync();
                }
                catch
                {
                    // Ignore disposal errors.
                }
            }
        }
    }

    extension(Task<IChannel> opening)
    {
        /// <summary>
        /// Closes the channel once the open completes. Does nothing when the open fails.
        /// </summary>
        /// <param name="connections">The manager that closes the channel.</param>
        public async Task CloseWhenOpenedAsync(RabbitMQConnectionManager connections)
        {
            try
            {
                await connections.CloseChannelAsync(await opening);
            }
            catch
            {
                // The channel could not be opened.
            }
        }
    }
}
