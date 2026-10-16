# RabbitMQ transport: connection state assessment

This report checks how the Mocha RabbitMQ transport (`src/Mocha/src/Mocha.Transport.RabbitMQ`) handles each connection, channel and consumer state that RabbitMQ.Client 7.x exposes, and where recovery can be improved.

It follows the fix for consumers the broker takes away while the connection stays up ([#10543](https://github.com/ChilliCream/graphql-platform/pull/10543)).

## Method

The findings come from reading the client source at tags v7.0.0 and v7.2.0, and from reading Mocha's code. Mocha's transport was built against 7.2.0 through a `VersionOverride`, and both Mocha and HotChocolate's `Subscriptions.RabbitMQ` now use 7.2.2 from `src/Directory.Packages.props`. The client's recovery decision logic is identical in 7.0.0 and 7.2.0. Only the rows marked "tested" are covered by tests. Everything else follows from reading the source and has not been reproduced.

## Coverage by state

| State or event | What the client does | What Mocha does | Verdict |
|---|---|---|---|
| Broker closes a consumer channel (406, 404 and others; consumer timeout before RabbitMQ 4.3) | Never recovers the channel. | Before #10543: nothing. Now: re-attaches the consumer with backoff and cancels the abandoned handlers. | Fixed, tested |
| Broker cancels a consumer (`basic.cancel`; consumer timeout from 4.3, deleted queue) | Puts the cancel in the consumer dispatcher queue, behind earlier deliveries. | Before #10543: nothing. Now: re-attaches the consumer and keeps retrying while the queue is missing. | Fixed, tested. One gap remains, see recommendation 1. |
| Connection lost (network loss, forced close, missed heartbeats) | Automatic recovery retries every 5 s without limit and restores channels and consumers. | Waits for the client's recovery, then replaces every recovered channel with a new one. | Two recovery mechanisms overlap, see recommendation 2. |
| Recovery takes longer than 30 s | `IsOpen` stays false during recovery. | The health monitor opens a new connection itself and disposes the old one. | Works since 7.2, where disposing stops the recovery loop. Still part of the overlap in recommendation 2. |
| First connection fails | Throws `BrokerUnreachableException`; automatic recovery does not apply. | Tries 5 times (about 15 s), then the host fails to start (open `TODO` in `RabbitMQMessagingTransport.OnBeforeStartAsync`). | Not resilient, see recommendation 5. |
| Connection blocked by the broker (memory or disk alarm) | Raises an event only. Publishes wait without limit once the write queue is full. | Logs the event. Publishes hang until the caller's token is cancelled. | Not handled, see recommendation 4. |
| RPC continuation timeout (20 s by default) | Throws `TimeoutException` and keeps the channel open. 7.0 left the channel unable to run further RPCs; 7.1 and later ignore the late reply instead. | The dispatcher returns the channel to its pool after any exception. | Acceptable on 7.2, but the pool should not keep channels that failed, see recommendation 4. |
| Publish rejected, unroutable or lost | Publisher confirms and `mandatory` are both off. | Publishes are fire and forget. | Messages can be lost without an error, see recommendation 4. |
| Ack of a delivery received before a connection recovery | Delivery tags from before the recovery are dropped without an error. | Not visible to Mocha. | Acceptable: the broker redelivers, so the cost is a duplicate. |
| Channels closed or consumers cancelled by the server | They stay recorded and are brought back on the next connection recovery. | #10543 closes and disposes lost channels, which removes them from the record. | Handled |

## Recommendations

In priority order.

### 1. Run handlers outside the client's consumer dispatcher

Mocha runs message handlers on the client's consumer dispatcher, which causes two problems:

- With `consumerDispatchConcurrency` 1, a blocked handler holds back a server-sent `basic.cancel`. On RabbitMQ 4.3 and later, a handler that retries in place without ever finishing therefore stops the cancel from arriving at all. The periodic health check cannot see the loss either, because the client updates `IsRunning` only when the cancel is dispatched. This is the one case #10543 does not fully cover.
- When the broker closes a channel, the client's reader loop waits until every delivery already queued for that channel has been handled. A handler that ignores its cancellation token therefore stops frame processing for the **whole consumer connection**, which means every queue. After about 180 s the missed heartbeats kill the connection. #10543 limits this by cancelling the abandoned handler's token first, which only helps handlers that honor the token.

Fix: return from the client callback right away, run handlers on Mocha's own workers, and enforce the concurrency limit with a semaphore. This fixes both problems.

### 2. Have a single owner for recovery

The client's automatic recovery restores channels and consumers, and Mocha's `OnConnectionRecoveredAsync` then closes them and opens new ones. Messages delivered in that window are redelivered. Mocha's 30 s health monitor also competes with the client's 5 s recovery loop.

Have Mocha own recovery. Either create its connections with `AutomaticRecoveryEnabled = false` (the `IConnectionFactory` currently comes from the application), or keep the channels the client recovered.

### 3. Keep RabbitMQ.Client at 7.2 or later

Done: both projects now use 7.2.2. Versions before 7.2 have bugs on paths Mocha uses: `DisposeAsync` does not stop the recovery loop (which leaks a connection when the health monitor takes over), a failed handshake starts a recovery loop on an unowned connection, a cancelled publish with confirms enabled releases a semaphore it never acquired, an RPC timeout leaves the channel unable to run RPCs, and `CloseAsync` can deadlock intermittently. The client is pinned centrally, so a future override must not go below 7.2.

### 4. Harden the publish path

- Enable publisher confirms and set `mandatory`, or make both configurable, so that rejected and unroutable publishes surface as errors.
- Dispose a pooled channel on any exception instead of returning it. `RabbitMQDispatchEndpoint.DispatchAsync` returns the channel in `finally` regardless of the outcome, so a channel the broker is about to close (for example after a publish to a missing exchange) can be handed to the next publisher.
- Track the connection's blocked state, so publishes fail fast or time out instead of hanging.

### 5. Make startup resilient

Keep retrying the first connection in the background and report the transport as unhealthy, instead of failing after 5 attempts. Start the health monitor even when the first connection attempt fails.

### 6. Expose consumer health

Add a health check that fails while any registered consumer is not attached. Such a check would have turned the original silent stall into a pod restart.

## Notes on #10543

- When the connection is lost, every consumer now logs a Warning and has its handlers cancelled. Re-attaching is then left to connection recovery.
- After a connection recovery, the periodic health check skips consumers that are already attached, so it does not re-attach them a second time.
- There is no test that waits for a real consumer timeout. RabbitMQ checks the timeout only once a minute, so it fires after about two minutes on 3.x. A CI matrix on 3.12, 4.1 and 4.3 with a `consumer-timeout` policy would cover the version-specific behavior end to end.
