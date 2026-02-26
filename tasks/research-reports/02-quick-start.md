Now I have all the information I need. Let me compile the comprehensive research report.

---

## Research Report: Mocha Quick Start — Competitor Analysis and Improvement Recommendations

---

## Current Page Summary

**File:** `/home/pascal/kot/graphql-platform/website/src/docs/mocha/v1/quick-start.md`

**What it covers:**
The Mocha quick start walks a developer from zero to a working ASP.NET Core app that publishes an `OrderPlaced` event and handles it — all in-process using the InMemory transport. The page promises "under five minutes" and "about 40 lines of code" in its description.

**Structure (7 sections):**

1. Prerequisites (.NET 9 SDK)
2. Create the project (`dotnet new web`)
3. Install the packages (2 packages)
4. Define a message (`OrderPlaced.cs`, a plain C# class)
5. Create a handler (`IEventHandler<T>` implementation)
6. Register the bus (`Program.cs` with `AddMessageBus()`, `AddEventHandler<>()`, `AddInMemory()`)
7. Publish and verify (`dotnet run` + curl)
8. Troubleshooting (3 common errors)
9. Next steps (links to 3 other docs pages)

**Scenario:** An e-commerce order placement flow — developer POSTs to `/orders`, the bus publishes `OrderPlaced`, the handler logs the order details.

**Transport:** InMemory. Explicitly explained as infrastructure-free.

**Approximate line count across all files:** ~80 lines of C# total across `Program.cs`, `OrderPlaced.cs`, and `OrderPlacedHandler.cs`.

**Time to "it works":** 5 minutes if prereqs are met. Verification is curl + console log output.

---

## Competitor Analysis

### NServiceBus (Particular Software)

**Reference:** https://docs.particular.net/tutorials/quickstart/ and https://docs.particular.net/tutorials/nservicebus-step-by-step/1-getting-started/

**Quick start approach:**
NServiceBus offers two parallel paths: a **Quickstart** (download a pre-built solution, press F5) and a **Step-by-Step Tutorial** (build from scratch, 10-15 minutes per lesson).

The Quickstart is deliberately zero-friction: download a `.zip` file, extract it, open `RetailDemo.sln`, and run it. No code is written by the reader in part one — they observe a working system. The scenario is a retail ordering system: `ClientUI` sends a `PlaceOrder` command to `Sales`, which publishes `OrderPlaced` to `Billing`. Three separate console processes, five projects total. The quickstart has no prerequisites except "a compatible IDE" — no broker, no database.

The Step-by-Step Tutorial lesson 1 takes 10-15 minutes, creates a single `ClientUI` console app, uses the **Learning Transport** (file-based queues in a `.learningtransport` folder), and produces roughly 13 lines of `Program.cs` configuration code. It ends with the endpoint running interactively.

**What they do WELL:**

- The "download and run" quickstart eliminates all setup friction — developers see a working multi-service system in under 2 minutes.
- Clear visual workflow diagrams show how messages flow between services.
- Three-part structure lets readers stop at "it works" (part 1), then optionally explore fault tolerance (part 2) and extending the system (part 3).
- The step-by-step tutorial calls out what each configuration line does explicitly.
- No external infrastructure is needed (Learning Transport uses the filesystem).

**What they do POORLY:**

- The quickstart builds understanding by showing, not doing — the reader writes no code in part 1, which is fine for exploration but leaves gaps for developers who learn by building.
- The pre-built solution obscures how things are wired together. Developers who start with the download may not know how to wire a fresh project.
- The Learning Transport (file-based) is significantly different from production transports, which can create a misleading mental model.
- The step-by-step tutorial is verbose — many explanation blocks interrupt the flow of actually getting something running.

---

### MassTransit

**Reference:** https://masstransit.io/quick-starts/in-memory and https://masstransit.io/quick-starts

**Quick start approach:**
MassTransit has a dedicated Quick Starts landing page that recommends starting with InMemory and then switching to a real transport. Their InMemory quick start is **8 steps**, using a custom project template (`dotnet new install MassTransit.Templates`, then `dotnet new mtworker` and `dotnet new mtconsumer`).

The scenario is abstract: a `BackgroundService` publishes a `GettingStarted` message every second containing a timestamp, and a consumer logs each received message as "Received Text: The time is [timestamp]". The expected output shows repeated log lines every second.

**What they do WELL:**

- The quick starts landing page organizes by transport (RabbitMQ, Azure Service Bus, Amazon SQS, PostgreSQL) so developers can immediately jump to their target infrastructure.
- The in-memory quick start is truly minimal — 8 steps, no infrastructure.
- Including a video alongside the text instructions gives multiple learning modalities.
- The step-by-step structure is clear and ordered.
- The statement "Everything builds off of the in-memory so start here" provides good navigational guidance.

**What they do POORLY:**

- The scenario (publishing a timestamp every second) demonstrates mechanics but builds nothing meaningful. The reader doesn't come away with an understanding of why messaging is useful — there's no business story.
- Using custom templates (`dotnet new mtworker`, `dotnet new mtconsumer`) hides the wiring. Readers don't understand what code the template generated, which makes it harder to adapt later.
- No explanation of what a consumer _is_ conceptually before showing how to create one.
- No verification step that meaningfully confirms correctness beyond "look for log lines."
- The `BackgroundService` pattern shown is worker-service-centric, not web-API-centric, which may not match how most developers are building new services in 2024+.
- Lack of error handling examples.

---

### Wolverine

**Reference:** https://wolverinefx.net/introduction/getting-started and https://wolverinefx.net/guide/messaging/introduction.html

**Quick start approach:**
Wolverine's getting started guide is ~6-7 steps. It builds a "simple issue tracking system" with two API endpoints: one to create an issue (`CreateIssue` command) and one to assign it (`AssignIssue` command). Handlers cascade — `CreateIssueHandler` stores the issue and returns an `IssueCreated` event, which triggers `IssueCreatedHandler` (a static method) that sends an email notification. The transport is Wolverine's **local in-memory queues**.

The code is approximately 70 lines across multiple files. The key differentiator: Wolverine discovers handlers **by naming convention** — no explicit registration of individual handlers in DI. Method injection lets handlers declare dependencies as parameters without constructor injection boilerplate.

**What they do WELL:**

- The issue-tracking scenario is more relatable than a timestamp pump and tells a clear business story (create issue -> notify via email).
- Cascading messages are demonstrated immediately, which shows Wolverine's distinguishing power early.
- Convention-based handler discovery means `Program.cs` is radically simpler than competitors.
- Integration with Minimal APIs is shown naturally — HTTP endpoints call `IMessageBus.InvokeAsync()` directly.
- The scenario flows from request to response with a logical chain readers can trace.

**What they do POORLY:**

- No explicit "run the app and verify it works" step — the guide ends with code but doesn't walk through the curl command or show expected output.
- Stays entirely in-process; no roadmap to distributed messaging scenarios.
- Limited explanation of the handler discovery mechanism — convention-over-configuration is powerful but can confuse developers coming from explicit registration frameworks (MassTransit, NServiceBus).
- No troubleshooting section.
- No quantified time estimate ("you'll be done in X minutes").

---

## Comparative Summary Table

| Dimension                 | Mocha                 | NServiceBus                                    | MassTransit           | Wolverine            |
| ------------------------- | --------------------- | ---------------------------------------------- | --------------------- | -------------------- |
| Steps to "it works"       | 7                     | 1 (download + run) / 5 (build from scratch)    | 8                     | 6-7                  |
| Time to working result    | ~5 min                | ~2 min (quickstart) / 10-15 min (step-by-step) | ~5-10 min             | Unquantified         |
| Transport                 | InMemory              | Learning (file-based)                          | InMemory              | Local queues         |
| Scenario quality          | Strong (order domain) | Strong (retail domain)                         | Weak (timestamp pump) | Good (issue tracker) |
| Code written by reader    | Yes (~80 lines)       | No (quickstart) / Yes (~13 lines)              | Yes via templates     | Yes (~70 lines)      |
| Explicit expected output  | Yes (JSON + log line) | Yes (console log format)                       | Yes (log lines)       | No                   |
| Troubleshooting section   | Yes (3 cases)         | Yes (extensive)                                | No                    | No                   |
| Next steps                | Yes (3 links)         | Yes (2 tutorials)                              | Partial               | Partial              |
| Custom templates required | No                    | No                                             | Yes                   | No                   |

---

## Best Practices Found

From research across Diátaxis (https://diataxis.fr/tutorials/), Pluralsight (https://www.pluralsight.com/resources/blog/software-development/tech-documentation-best-practices), and draft.dev (https://draft.dev/learn/documentation-best-practices-for-developer-tools):

1. **Show the destination at the top.** Tell the reader exactly what they will have built and what they will see when it works, before any steps begin. Diátaxis explicitly calls this out: "providing the picture the learner needs...as simple as informing them at the outset."

2. **Deliver visible results frequently.** Each step should produce an observable outcome. The reader should not have to wait until the final step to see evidence they're on track.

3. **Minimize explanation ruthlessly.** Tutorials are not the place for theory. Link outward to concept docs. Keep the reader in motion.

4. **"Time to Hello World" is a key metric.** The faster a reader can produce a working result, the lower the abandonment rate. 5 minutes is the widely-cited target.

5. **Use a domain-meaningful scenario.** Timestamp pumps and counter increments demonstrate mechanics but not value. Use a business scenario (order placed, issue created) so readers understand _why_ they'd use the tool.

6. **Show exact expected output.** Always show what the console, log, or API response looks like when the tutorial succeeds. This is the reader's primary confidence signal.

7. **Include a troubleshooting section.** The most common failure modes should be documented. Readers who get stuck and find their exact error in the docs are far more likely to continue.

8. **End with "here's what to do next."** A quick start should not end at "Success!" It should end with a curated set of next steps based on what the developer most likely wants to do after the initial success.

9. **No external infrastructure for the quick start.** InMemory / in-process is the correct choice for quick starts. Real transport quick starts belong in a separate "Transport" section.

10. **Avoid requiring custom templates.** Custom `dotnet new` templates hide the wiring. Readers learn better by writing the code themselves (even if minimal) than by having a template generate it.

---

## External References

**Competing Framework Docs:**

- NServiceBus Quickstart: https://docs.particular.net/tutorials/quickstart/
- NServiceBus Step-by-Step Tutorial: https://docs.particular.net/tutorials/nservicebus-step-by-step/1-getting-started/
- NServiceBus Get Started Hub: https://docs.particular.net/get-started/
- MassTransit Quick Starts Hub: https://masstransit.io/quick-starts
- MassTransit InMemory Quick Start: https://masstransit.io/quick-starts/in-memory
- Wolverine Getting Started: https://wolverinefx.net/introduction/getting-started
- Wolverine Messaging Introduction: https://wolverinefx.net/guide/messaging/introduction.html

**Enterprise Integration Patterns (EIP):**

- Event Message pattern: https://www.enterpriseintegrationpatterns.com/patterns/messaging/EventMessage.html — directly relevant; the `OrderPlaced` event in the quick start is an EIP Event Message. The EIP page notes that "the notification's arrival matters more than detailed information," which justifies the minimal payload in the quick start's `OrderPlaced` class.
- Publish-Subscribe Channel: https://www.enterpriseintegrationpatterns.com/patterns/messaging/PublishSubscribeChannel.html — the quick start demonstrates this pattern: one publisher, one subscriber, decoupled via the bus.

**Documentation Best Practices:**

- Diátaxis framework (tutorials): https://diataxis.fr/tutorials/ — the most rigorous framework for thinking about what a tutorial should and should not do.
- draft.dev documentation best practices: https://draft.dev/learn/documentation-best-practices-for-developer-tools
- Pluralsight tech documentation best practices: https://www.pluralsight.com/resources/blog/software-development/tech-documentation-best-practices

**.NET Hosting Context:**

- Andrew Lock on IHostedService ordering: https://andrewlock.net/controlling-ihostedservice-execution-order-in-aspnetcore-3/ — relevant to the `runtime.StartAsync` usage in the quick start (see recommendations below).
- Microsoft docs on hosted services: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services — context for the messaging runtime lifecycle.

---

## Recommendations for the Mocha Quick Start

**Strengths to preserve:**

- The `OrderPlaced` scenario is the best among all three competitors — it's a domain-meaningful business event that readers can immediately relate to.
- Explicit expected output (both the JSON response and the log line) is excellent. Don't remove this.
- The troubleshooting section is a differentiator. NServiceBus has one; MassTransit and Wolverine do not.
- No custom templates required — readers write every line themselves and understand what they wrote.
- The explanation table for `AddMessageBus()` / `AddEventHandler<>()` / `AddInMemory()` / `runtime.StartAsync()` is a useful quick-reference.

**Issues to address:**

**1. The `runtime.StartAsync` call is a code smell that will confuse readers.**

```csharp
// This pattern requires the reader to cast to a concrete type and call StartAsync manually:
var runtime = (MessagingRuntime)app.Services
    .GetRequiredService<IMessagingRuntime>();
await runtime.StartAsync(CancellationToken.None);
```

This pattern bypasses ASP.NET Core's `IHostedService` infrastructure. In real .NET hosting, services registered as `IHostedService` are started automatically when `app.Run()` is called — the developer never calls `StartAsync` manually. The explicit cast to `MessagingRuntime` is particularly alarming: why not `IMessagingRuntime`? The `Warning` callout about "silently dropped" events if you forget to call this is evidence that the API design has a sharp edge. Recommendation: either (a) make `MessagingRuntime` register itself as an `IHostedService` so `app.Run()` starts it automatically, or (b) provide a `UseMessageBus()` extension on `WebApplication` that handles this, similar to how `UseWolverine()` works. The quick start should not expose this wiring complexity.

**2. The promise of "40 lines of code" in the description doesn't match reality.**
The description says "about 40 lines of code," but the actual code spans three files with a combined ~80 lines including blank lines and comments. The description should either be removed or updated to accurately describe what's written (e.g., "three files, roughly 80 lines total including comments").

**3. The opening paragraph should lead with the outcome more visually.**
Currently the guide starts with a prose sentence. NServiceBus and Wolverine both use diagrams or explicit scenario descriptions to set expectations. Consider adding a one-line outcome statement and a brief message-flow description before the prerequisites:

```
OrderPlaced event
  → Publisher (POST /orders)
  → InMemory bus
  → OrderPlacedHandler (logs the order)
```

This gives the reader a mental model before they start writing code, which Diátaxis identifies as critical.

**4. The handler registration is manual and verbose relative to competitors.**

```csharp
builder.Services
    .AddMessageBus()
    .AddEventHandler<OrderPlacedHandler>()  // must be called for each handler
    .AddInMemory();
```

Wolverine's convention-based auto-discovery means `Program.cs` has no per-handler registration. MassTransit's consumers are also auto-discovered. If Mocha requires explicit registration of every handler, the quick start should acknowledge this and explain that there will be assembly-scanning alternatives (even if they're in a different doc page). Without this, readers will wonder if they have to add an `AddEventHandler<>()` call for every handler in their production app.

**5. The curl command will break on some systems due to the port.**

```bash
curl -X POST http://localhost:5000/orders
```

ASP.NET Core's default port in `dotnet new web` has shifted over SDK versions. In some configurations it uses a random HTTPS port or port 5001. The guide should either show how to confirm the port from the startup log, or explicitly pin the port with a `launchSettings.json` snippet or `--urls` flag.

**6. No "what just happened?" explanation after verification.**
After showing the expected output, Mocha says "If you see that log line, it worked." But the reader doesn't fully understand the execution path: their POST request hit the endpoint, the endpoint called `PublishAsync`, the bus routed the event to the handler based on the type match, and the handler was invoked on a background thread. A 3-4 sentence "What just happened?" paragraph immediately after the verification block would reinforce the core mental model before sending the reader to Next Steps.

**7. The "Next steps" section is good but could be more opinionated.**
Currently it lists three equally-weighted links. NServiceBus's approach is more effective: they tell the reader which path to take based on what they want to do next. Consider rephrasing:

- "Want to understand the full power of the handler pipeline? Read Handlers and Consumers."
- "Ready to move to production? See Transports to connect RabbitMQ."
- "Need to write tests for your handlers? Read Testing."
