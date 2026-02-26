I now have all the research needed. Here is the full structured report.

---

# Research Report: Mocha Testing Documentation vs. Competitors

## Current Page Summary

**File:** `/home/pascal/kot/graphql-platform/website/src/docs/mocha/v1/testing.md`

**Description:** "Learn how to test message-driven code in Mocha using the InMemory transport."

### Structure and Content

The page covers seven distinct scenarios in this order:

1. Publish an event and assert a handler ran (with `MessageRecorder` helper)
2. Test a request/reply flow (synchronous assertion on return value)
3. Test a send/fire-and-forget command (with `MessageRecorder`)
4. Test fan-out to multiple handlers (keyed DI services)
5. Test custom header propagation (with `HeaderCapture` helper)
6. Test saga state transitions (with `SagaTester<T>` helper)
7. Build a reusable test fixture (`TestBus` static factory)

Plus two supporting sections: "Why InMemory is the recommended test transport" and a Troubleshooting section covering three common failure modes.

### Core Technical Approach

- All tests use the same `AddMessageBus()` + `AddInMemory()` DI setup as production code
- Async synchronization is handled by a manually-implemented `MessageRecorder` using `SemaphoreSlim`
- Each test creates its own `ServiceProvider` for isolation
- Saga unit testing uses a dedicated `SagaTester<T>` that runs without the full bus
- The page teaches integration testing with the InMemory transport as the primary strategy; no separate "unit test handlers in isolation" path is shown
- Every test example requires roughly 10-15 lines of setup boilerplate before the Act step

### Notable Strengths of the Current Page

- Complete, runnable code examples for every scenario
- Good Troubleshooting section with three concrete failure modes
- The "Why InMemory" section explicitly addresses the tradeoff between InMemory and real broker tests
- Test isolation guarantee is explicitly explained with a code example showing the fix

### Notable Weaknesses of the Current Page

- The `MessageRecorder` helper must be implemented by the user with no out-of-box version provided
- The `MessagingRuntime` cast (`(MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>()`) appears in every single test and is awkward boilerplate
- There is no discussion of the testing pyramid or when to prefer unit testing handler logic vs. full integration testing
- No guidance on testing error handling, exceptions, or dead-letter behavior
- The `SagaTester<T>` section does not show what happens when a saga sends or publishes outbound messages, only state transitions
- No mention of how to test middleware or pipeline behaviors
- No `IClassFixture` or `CollectionFixture` guidance for xUnit users who want shared bus instances across test classes

---

## Competitor Analysis

### NServiceBus (Particular Software)

**Documentation URLs:**

- [Testing NServiceBus](https://docs.particular.net/nservicebus/testing/)
- [Saga scenario testing](https://docs.particular.net/nservicebus/testing/saga-scenario-testing)
- [Unit Testing NServiceBus samples](https://docs.particular.net/samples/unit-testing/)

**Testing Philosophy:** NServiceBus treats unit testing and integration testing as two distinct and equally documented paths. Their philosophy is to test handler _decisions_ (what messages to send/publish) via pure unit tests, reserving full endpoint testing for true end-to-end validation.

**Test Harness Provided:** Yes. The `NServiceBus.Testing` NuGet package ships `TestableMessageHandlerContext`, `TestableMessageSession`, and saga-specific context classes. These are in-process fakes that implement production interfaces.

**How Unit Testing Handlers Works:**

```csharp
// Instantiate handler directly — no DI container
var handler = new MyReplyingHandler();
var context = new TestableMessageHandlerContext();

await handler.Handle(new MyRequest(), context);

// Assert on captured outgoing operations
Assert.That(context.RepliedMessages, Has.Length.EqualTo(1));
Assert.That(context.RepliedMessages[0].Message, Is.InstanceOf<MyResponse>());
```

This is a fundamentally different pattern from Mocha's approach: no DI container, no transport, no async synchronization. The handler is called directly as a method, and the context captures the side effects synchronously. Setup boilerplate is approximately 3 lines.

**Saga Testing:** Two levels. Level 1 is handler-level testing using `TestableMessageHandlerContext` (same as handlers, just instantiate the saga and set `Data` directly). Level 2 is the `TestableSaga` scenario tester that:

- Exercises the correlation configuration (`ConfigureHowToFindSaga`)
- Maintains saga state across multiple message invocations
- Provides a virtual clock with `AdvanceTime()` to trigger stored timeouts
- Lets you `SimulateReply<TCommand, TResponse>()` to model external service responses
- Provides `FindSentMessage<T>()`, `FindPublishedMessage<T>()`, `FindTimeoutMessage<T>()`, `FindReplyMessage<T>()`

The scenario tester is the most sophisticated saga unit test harness of the three competitors.

**Boilerplate:** Very low for handler unit tests (3-5 lines setup). Slightly more for saga scenario tests but still minimal. No DI container required for unit tests.

**What They Do Well:**

- Clear separation of unit vs. integration testing with guidance on when to use each
- Extremely low boilerplate for handler unit tests
- The `TestableSaga` scenario tester handles time simulation, correlation, and multi-message flows
- Support for logging assertions via `TestingLoggerFactory`
- Explicit guidance on using the Learning Transport for integration tests without a real broker

**What They Do Poorly:**

- Unit tests don't exercise the pipeline/middleware — they call handlers directly, bypassing everything else
- The `TestableMessageHandlerContext` approach misses serialization/deserialization bugs
- Integration testing guidance is sparse in the official docs (relies on the community `NServiceBus.IntegrationTesting` package for true end-to-end scenarios)
- No explicit guidance on test isolation when tests do use the actual endpoint

---

### MassTransit

**Documentation URLs:**

- [Testing (concepts)](https://masstransit.io/documentation/concepts/testing)
- [Test Harness (configuration)](https://masstransit.io/documentation/configuration/test-harness)

**Testing Philosophy:** MassTransit takes a middle path. It provides `AddMassTransitTestHarness()` which plugs into the existing DI registration and swaps in an in-memory transport. The test harness is the primary testing mechanism — there is no separate "call handler directly" unit test path officially documented.

**Test Harness Provided:** Yes. `AddMassTransitTestHarness()` is a first-class, shipped testing feature. It provides `ITestHarness` which exposes:

- `harness.Sent` — messages sent
- `harness.Consumed` — messages consumed
- `harness.Published` — messages published
- `harness.GetSagaStateMachineHarness<TStateMachine, TState>()` — saga-specific harness

**How Unit Testing Consumers Works:**

```csharp
await using var provider = new ServiceCollection()
   .AddYourBusinessServices()
   .AddMassTransitTestHarness(x =>
   {
       x.AddConsumer<SubmitOrderConsumer>();
   })
   .BuildServiceProvider(true);

var harness = provider.GetRequiredService<ITestHarness>();
await harness.Start();

var client = harness.GetRequestClient<SubmitOrder>();
var response = await client.GetResponse<OrderSubmitted>(new
{
    OrderId = InVar.Id,
    OrderNumber = "123"
});

Assert.IsTrue(await harness.Sent.Any<OrderSubmitted>());
Assert.IsTrue(await harness.Consumed.Any<SubmitOrder>());
```

Boilerplate is approximately 8-10 lines. The `harness.Consumed.Any<T>()` call handles async waiting with a configurable `TestInactivityTimeout` (default: 1.2 seconds in production mode, 30 minutes while debugging). This is a clever design that eliminates the need for a manually coded `MessageRecorder`.

**Saga Testing:**

```csharp
await harness.Bus.Publish(new OrderSubmitted
{
    CorrelationId = sagaId,
    OrderNumber = orderNumber
});

var sagaHarness = harness.GetSagaStateMachineHarness<OrderStateMachine, OrderState>();
Assert.That(await sagaHarness.Consumed.Any<OrderSubmitted>());
Assert.That(await sagaHarness.Created.Any(x => x.CorrelationId == sagaId));

var instance = sagaHarness.Created.ContainsInState(sagaId,
    sagaHarness.StateMachine,
    sagaHarness.StateMachine.Submitted);

Assert.IsNotNull(instance);
```

The `GetSagaStateMachineHarness<T,S>()` method gives direct access to saga instances and their current state — a feature Mocha's `SagaTester<T>` also provides but through a different API surface.

**What They Do Well:**

- `harness.Consumed.Any<T>()` is a first-class async waiting primitive — no need to implement `MessageRecorder` manually
- The `TestInactivityTimeout` design (short in CI, long during debugging) is a clever ergonomic choice
- `GetSagaStateMachineHarness<T,S>()` with `ContainsInState()` enables direct saga state inspection
- `WebApplicationFactory` integration allows testing full ASP.NET Core applications with a real test server and in-memory transport
- `SetTestTimeouts()` is explicitly documented, encouraging teams to configure timeouts deliberately

**What They Do Poorly:**

- Despite the "simplification" claims, boilerplate is still significant — comparable to Mocha's approach
- The `harness.Consumed.Any<T>()` default timeout of 1.2 seconds is too short and causes flaky tests in slow CI environments; the docs warn about this but don't solve it
- No guidance on testing pipeline behaviors/middleware
- Limited guidance on complex saga interactions (compensation, concurrent events, race conditions)

---

### Wolverine

**Documentation URLs:**

- [Test Automation Support](https://wolverinefx.net/guide/testing.html)
- [Wolverine's Baked In Integration Testing Support (blog)](https://jeremydmiller.com/2024/03/25/wolverines-baked-in-integration-testing-support/)

**Testing Philosophy:** Wolverine is the most opinionated of the three. It explicitly advocates for integration testing as the primary strategy for any handler with outgoing messages or async behavior. Unit testing is supported for pure handler logic decisions but the docs treat it as a complement, not a replacement. The framework was designed from scratch with testability as a first-class concern.

**Test Harness Provided:** Yes, but not a separate NuGet package. The `TrackedSession` mechanism is built into the core framework via `IHost` extension methods.

**How Integration Testing Works (Tracked Sessions):**

```csharp
// No MessageRecorder, no polling loop, no timeout configuration needed
var debitAccount = new DebitAccount(111, 300);
var session = await host.InvokeMessageAndWaitAsync(debitAccount);

// Wolverine automatically waits for ALL cascading messages to complete
var overdrawn = session.Sent.SingleMessage<AccountOverdrawn>();
overdrawn.AccountId.ShouldBe(debitAccount.AccountId);
```

This is Wolverine's key differentiator. The tracked session monitors all in-flight message activity internally and returns only when every cascading message has been processed. This eliminates the root cause of flaky tests (timing) by making "done" deterministic rather than timeout-based.

Advanced configuration:

```csharp
var session = await host
    .TrackActivity()
    .Timeout(1.Minutes())
    .IncludeExternalTransports()
    .AlsoTrack(otherWolverineSystem)         // multi-process scenarios
    .DoNotAssertOnExceptionsDetected()
    .WaitForMessageToBeReceivedAt<LowBalanceDetected>(otherWolverineSystem)
    .InvokeMessageAndWaitAsync(debitAccount);
```

**How Unit Testing Handler Logic Works:**

```csharp
// Handler returns messages as IEnumerable<object> — no framework needed at all
var messages = AccountHandler.Handle(message, account, session).ToList();

// Extension method assertions on the returned message list
messages.ShouldHaveMessageOfType<AccountUpdated>().AccountId.ShouldBe(account.Id);
messages.ShouldHaveMessageOfType<LowBalanceDetected>()
    .AccountId.ShouldBe(account.Id);
messages.ShouldHaveNoMessageOfType<AccountOverdrawn>();
```

This is the most elegant unit testing approach of the three competitors. Because Wolverine handlers can return cascading messages as return values (rather than calling `context.Send()` / `context.Publish()`), unit testing is pure function testing with no mocking or fake contexts required. The `ShouldHaveMessageOfType<T>()` extension methods are provided by the framework.

**Saga/Stateful Workflow Testing:** The TrackedSession API works for sagas too; no special saga harness is called out, but the session tracks all saga-triggered messages automatically.

**What They Do Well:**

- TrackedSession is the most elegant solution to the async timing problem — "done" is deterministic, not timeout-driven
- Unit testing handler logic requires zero framework infrastructure when handlers return messages as values
- Multi-process testing (`AlsoTrack(otherHost)`) is documented for distributed scenarios
- `StubWolverineMessageHandling<TCommand, TResponse>()` provides a clean way to stub external message call-outs
- `DisableAllExternalWolverineTransports()` makes swapping transports for tests a one-liner
- Test diagnostics (formatted activity table showing every message event) aid debugging significantly

**What They Do Poorly:**

- The unit testing approach (returning messages as values) requires handler authors to adopt a specific coding style — existing code using `IMessageContext` directly cannot be unit-tested this way
- Less documentation on testing error/failure scenarios explicitly
- The `IHost`-based setup means tests start the full application host, which is heavier than MassTransit's or Mocha's `ServiceCollection`-only approach

---

## Comparative Feature Matrix

| Feature                             | Mocha                         | NServiceBus                           | MassTransit                         | Wolverine                                   |
| ----------------------------------- | ----------------------------- | ------------------------------------- | ----------------------------------- | ------------------------------------------- |
| Built-in test harness               | No (manual `MessageRecorder`) | Yes (`NServiceBus.Testing` package)   | Yes (`AddMassTransitTestHarness`)   | Yes (TrackedSession in core)                |
| Handler unit testing (no transport) | No                            | Yes (`TestableMessageHandlerContext`) | No (harness still required)         | Yes (return-value pattern)                  |
| Async waiting primitive             | Manual (`SemaphoreSlim`)      | N/A (sync-style tests)                | `harness.Consumed.Any<T>()`         | `InvokeMessageAndWaitAsync` (deterministic) |
| Saga testing                        | `SagaTester<T>`               | `TestableSaga` with time simulation   | `GetSagaStateMachineHarness<T,S>()` | TrackedSession                              |
| Time simulation for sagas           | Not documented                | Yes (`AdvanceTime()`)                 | Yes (virtual time scheduler)        | Not documented explicitly                   |
| Test setup boilerplate              | High (10-15 lines)            | Low (3-5 lines)                       | Medium (8-10 lines)                 | Low-Medium (depends on approach)            |
| Transport swap                      | `AddInMemory()`               | Learning Transport                    | In-memory (automatic in harness)    | `DisableAllExternalWolverineTransports()`   |
| WebApplicationFactory support       | Not mentioned                 | Not mentioned                         | Yes (explicitly documented)         | Via IHost (implicit)                        |
| Troubleshooting section             | Yes (3 scenarios)             | Minimal                               | Minimal                             | Via diagnostic output                       |
| Testing pipeline/middleware         | Not mentioned                 | Not mentioned                         | Not mentioned                       | Not mentioned                               |

---

## Best Practices Found from External Research

**1. Deterministic async synchronization over polling**

The most common cause of flaky async tests is relying on `Task.Delay` or fixed timeouts. The superior patterns are:

- Signal-based synchronization (Mocha's `SemaphoreSlim` in `MessageRecorder` is correct)
- "Completion detection" (Wolverine's TrackedSession — don't timeout, detect actual completion)
- Activity-based inactivity timeout (MassTransit's `TestInactivityTimeout` — wait until no new activity, not until a wall clock expires)

**2. Separate unit and integration testing concerns**

NServiceBus's documentation makes the clearest case for this separation: unit tests verify _what decisions a handler makes_ (what it sends/publishes/replies to, given inputs), while integration tests verify _that the pipeline correctly routes and delivers messages_. Mocha's current docs only show the integration test path.

**3. Test pyramid applies to messaging too**

From [martinfowler.com's microservices testing guide](https://martinfowler.com/articles/microservice-testing/), the five test levels are: unit, integration, component, contract, end-to-end. For messaging systems specifically, a healthy distribution favors many cheap unit tests of handler logic, a moderate number of in-process integration tests (InMemory transport), and a small number of broker-level tests.

**4. The InMemory transport tradeoff is a documented best practice**

Both MassTransit and Mocha make this tradeoff explicit: InMemory tests exercise the pipeline, not the transport. This is the correct position. NServiceBus's Learning Transport is their equivalent. Wolverine disables external transports entirely in tests rather than substituting a fake one.

**5. Saga testing requires multi-message scenario support**

NServiceBus's `TestableSaga.AdvanceTime()` and Wolverine's TrackedSession (which follows cascading messages) are both addressing the same real problem: saga testing that covers only a single message invocation does not test the scenario — it tests a single state transition. Meaningful saga tests must exercise sequences of events. Mocha's `SagaTester<T>` page shows only single-event transitions in the example.

**6. Test isolation is a critical and commonly missed concern**

All three competitors address this. Mocha's Troubleshooting section covers it well. The correct pattern (one `ServiceProvider` per test) is the same across all frameworks.

---

## External References

**Competitor Documentation:**

- [NServiceBus Testing](https://docs.particular.net/nservicebus/testing/)
- [NServiceBus Saga Scenario Testing](https://docs.particular.net/nservicebus/testing/saga-scenario-testing)
- [NServiceBus Unit Testing Samples](https://docs.particular.net/samples/unit-testing/)
- [MassTransit Testing (Concepts)](https://masstransit.io/documentation/concepts/testing)
- [MassTransit Test Harness (Configuration)](https://masstransit.io/documentation/configuration/test-harness)
- [Wolverine Test Automation Support](https://wolverinefx.net/guide/testing.html)
- [Wolverine's Baked In Integration Testing Support (blog)](https://jeremydmiller.com/2024/03/25/wolverines-baked-in-integration-testing-support/)

**Testing Best Practices:**

- [Testing Strategies in a Microservice Architecture (Martin Fowler)](https://martinfowler.com/articles/microservice-testing/)
- [The Practical Test Pyramid (Martin Fowler)](https://martinfowler.com/articles/practical-test-pyramid.html)
- [The New and Improved NServiceBus Testing Framework (Particular blog)](https://particular.net/blog/the-new-and-improved-nservicebus-testing-framework)
- [Wolverine's Test Support Diagnostics (Jeremy Miller blog)](https://jeremydmiller.com/2024/05/16/wolverines-test-support-diagnostics/)
- [NServiceBus Integration Testing NimblePros](https://blog.nimblepros.com/blogs/testing-nservicebus-message-handlers/)
- [Integration Testing an HTTP Service with Wolverine](https://jeremydmiller.com/2023/07/09/integration-testing-an-http-service-that-publishes-a-wolverine-message/)

---

## Recommendations for Improving testing.md

**1. Provide `MessageRecorder` as a built-in, or eliminate the need for it entirely**

Every test that uses fire-and-forget or publish requires users to implement `MessageRecorder` themselves. MassTransit solves this with `harness.Consumed.Any<T>()`. Mocha should either ship a `MessageRecorder` or `TestHarness` type in a `Mocha.Testing` package, or document a waiting primitive that doesn't require hand-rolling a `SemaphoreSlim`. If the framework cannot be changed, the page should at minimum provide `MessageRecorder` as a copy-paste utility class that users install once per test project, not implement per test.

**2. Extract and kill the repeated `MessagingRuntime` cast**

The pattern `(MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>()` appears in every single test example. This is an API smell that leaks implementation details into user tests. The `TestBus.CreateAsync()` fixture hides this correctly, but the fixture itself should also hide it. Consider whether `IMessagingRuntime` could expose `StartAsync` directly, or whether a `provider.StartMessagingAsync()` extension method should be documented as the canonical way to start in tests.

**3. Add a "Unit testing handler logic" section before the integration test examples**

NServiceBus and Wolverine both document a way to test handler _decisions_ without standing up any transport. This is valuable for fast feedback loops. Even if Mocha doesn't provide a `TestableMessageHandlerContext` equivalent, the page should show how to test a handler's logic by calling the handler directly:

```csharp
// Direct handler invocation — no bus, no transport, no DI
var recorder = new MessageRecorder();
var handler = new OrderCreatedHandler(recorder);
await handler.HandleAsync(new OrderCreated { OrderId = "ORD-1" }, CancellationToken.None);
Assert.Single(recorder.Messages);
```

This is 5 lines instead of 15 and runs in microseconds. Then position the InMemory transport tests as the "integration test" layer for when the pipeline matters.

**4. Add a testing philosophy introduction paragraph**

None of Mocha's current page explains _when_ to use InMemory integration tests vs. plain handler unit tests vs. real broker tests. NServiceBus has the clearest guidance on this. Adding a brief "When to use each approach" table or paragraph at the top would help teams make informed choices. Example framing:

- Call the handler directly: when testing handler decision logic (what it sends, publishes, replies to)
- Use InMemory transport: when testing routing, middleware, or end-to-end message flow
- Use the real broker: when testing transport-specific configuration (exchange bindings, queue TTLs)

**5. Expand the `SagaTester<T>` section to show multi-event scenarios and outbound message assertions**

The current example only shows state transitions. A complete example should also show:

- Calling `tester.ExpectSentMessage<T>()` after a transition that triggers a send
- A sequence of two or more events to demonstrate `SetState()` for mid-flow entry
- What happens when a saga event is received in the wrong state (demonstrating that `SagaTester` protects against this)

**6. Add a section on testing error handling**

None of the three competitors document this well, which is a gap across the industry. Mocha's page should address:

- What happens when a handler throws? Does `RequestAsync` surface the exception?
- How to test that a dead-letter or retry scenario is triggered
- How to assert on the exception type when `RequestAsync` throws a `RemoteErrorException`

The current page mentions `RemoteErrorException` in passing but doesn't show how to test error paths.

**7. Add xUnit `IClassFixture` guidance for shared bus setup**

xUnit users commonly ask whether they can share a bus instance across multiple tests in a class using `IClassFixture<T>`. The current page says "each test creates its own ServiceProvider" and leaves it there. Adding a section showing how to safely use `IClassFixture` with `IAsyncLifetime` for startup/teardown would prevent a common mistake.

**8. Add a `CollectionDefinition` note for performance-sensitive suites**

For large test suites, create a note that all InMemory bus tests can run in parallel (distinct `ServiceProvider` instances) without collection isolation — unlike database or external broker tests. This is an important performance hint for teams with hundreds of message tests.
