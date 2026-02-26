Now I have comprehensive research to produce the structured report. Let me compile everything.

---

## Research Report: Middleware and Pipelines Documentation

### Current Page Summary

**File:** `/home/pascal/kot/graphql-platform/website/src/docs/mocha/v1/middleware-and-pipelines.md`

**Structure (391 lines):**

1. Opening paragraph anchoring to ASP.NET Core analogy
2. Pipeline overview list (Dispatch, Receive, Consumer)
3. ASCII art diagram showing the three-pipeline architecture
4. Conceptual explanation of `next(context)` and short-circuiting
5. Three authoring walkthroughs (receive, dispatch, consumer), each with define + register pattern
6. Ordering reference: method table + built-in key table
7. Built-in middleware reference (all three pipelines, each middleware described)
8. Compiler internals explanation (how the delegate chain is built at startup)
9. "Why three separate pipelines" architectural rationale
10. Troubleshooting section (3 common failure modes)
11. Next steps links

**Content strengths:** Comprehensive, covers both "how to use" and "why it works this way," has troubleshooting, has an ASCII diagram, uses real-world motivating examples (tenant headers, timing), and explicitly names all built-in middleware with their keys.

**Content weaknesses:**

- The ASP.NET Core analogy at the top is stated but never visualized — the page does not reproduce the iconic "before/after next" nesting diagram that makes ASP.NET Core middleware intuitive.
- The ASCII diagram shows the three pipeline stages stacked vertically but does not show a horizontal nested-wrapping view (the "matryoshka" or "onion" model), which is the mental model developers actually need to understand execution order.
- The pipeline concept is treated as a standalone page rather than being woven into adjacent pages (Reliability, Observability, Routing). Those pages describe behavior without ever referencing where in the pipeline that behavior sits.
- The `Create()` factory pattern requires boilerplate that is not motivated. Readers encounter the static factory before understanding why it exists (the answer: dependency injection scoping per-message, which is explained in the compiler section 200 lines later).
- Consumer middleware is shown third even though it is the entry point for the most common use case (wrapping handler execution). Dispatch is a less common customization point for most users.

---

### Competitor Analysis

#### NServiceBus (Particular Software)

**Documentation structure:**

- Hub page: `docs.particular.net/nservicebus/pipeline/` — an index linking to sub-pages
- "Manipulate pipeline with behaviors": `docs.particular.net/nservicebus/pipeline/manipulate-with-behaviors`
- "Steps, Stages and Connectors": `docs.particular.net/nservicebus/pipeline/steps-stages-connectors`
- 10 runnable samples: `docs.particular.net/samples/pipeline/`

**Key differences from Mocha:**

NServiceBus uses **Mermaid diagrams** on the steps/stages page to show the incoming and outgoing pipelines, with stages (Transport Receive → Physical Message → Logical Message → Handler Invocation) shown as distinct conceptual layers. This immediately communicates that the pipeline is not flat — it has hierarchical context transitions where the context bag is cloned at stage boundaries and data does not flow back upstream.

The "Behaviors" page anchors to the `Behavior<TContext>` base class and then progressively introduces: basic wrapping, conditional registration, feature-based registration, dependency injection as singleton-by-nature (with explicit warning), context extension bags for sharing state between behaviors.

The "Unit of work" sample is a standout: it shows a complete, production-realistic use case (database transaction wrapping) rather than trivial logging examples. This teaches the pattern in context.

**What they do well:**

- The samples library (10 entries) is the strongest documentation asset. Each sample is a runnable project demonstrating a real business scenario. Documentation by working example is more persuasive than any textual description.
- The singleton warning about behavior dependencies is an explicit, prominently placed caution that Mocha's page lacks entirely — Mocha's factory pattern has the same trap if you resolve a scoped service incorrectly.
- The "Behaviors vs Mutators" comparison table helps developers choose the right extension point.
- The steps/stages/connectors page explicitly teaches the context-cloning rule, which explains why state does not propagate back through the pipeline.

**What they do poorly:**

- The hub page (pipeline index) has no diagram at all. You must click into sub-pages to get visual explanations.
- The manipulate-with-behaviors page has no troubleshooting section and no common mistakes guide.
- Performance implications of deep behavior stacks are not addressed.
- The three pipelines (incoming, outgoing, recoverability) are not presented as a unified mental model on one page — developers must navigate across several pages to understand how they connect.

#### MassTransit

**Documentation structure:**

- Main page: `masstransit.io/documentation/configuration/middleware`
- Filters sub-page: `masstransit.io/documentation/configuration/middleware/filters`
- Scoped filters: `masstransit.io/documentation/configuration/middleware/scoped`
- Transaction filter: `masstransit.io/documentation/configuration/middleware/transactions`

**Key differences from Mocha:**

MassTransit uses the formal **Pipes and Filters** name from enterprise integration patterns. Every middleware component is a "filter" that implements `IFilter<T>`, and pipelines are `IPipe<T>` compositions. This naming choice is deliberate — it connects to a well-known architectural vocabulary.

The core filter interface is:

```csharp
public interface IFilter<TContext>
    where TContext : class, PipeContext
{
    void Probe(ProbeContext context);
    Task Send(TContext context, IPipe<TContext> next);
}
```

The `Probe` method (for diagnostic introspection) has no equivalent in Mocha — MassTransit filters are self-documenting because each filter can report its configuration to a diagnostic probe. This is an architectural pattern worth noting.

The middleware page groups built-in filters into categories: Kill Switch, Circuit Breaker, Rate Limiter, Concurrency Limit. Each has a configuration options table. This is more structured than Mocha's inline prose descriptions for each built-in middleware.

The `UseKillSwitch` / `UseCircuitBreaker` naming convention (method name starts with `Use`) is consistent throughout MassTransit, making the middleware API discoverable via IntelliSense. Mocha uses `UseReceive`, `PrependReceive`, `AppendReceive` which is more expressive but harder to discover.

**What they do well:**

- Formal naming (filters, pipes) ties directly to the EIP Pipes and Filters pattern, giving developers a theoretical foundation they can look up independently.
- The configuration options tables (thresholds, timeouts, per-filter) are complete reference material.
- `IFilter<T>` is a clean interface that is easy to implement and test in isolation.
- Scoped filters are explicitly documented as a first-class concept with a dedicated page.

**What they do poorly:**

- No visual diagram anywhere in the middleware documentation. The page describing a pipeline has no picture of the pipeline.
- The relationship between `IPipe<T>` and `IFilter<T>` is described but not visualized.
- No explanation of execution order when multiple filters are registered on the same pipe.
- No troubleshooting section. No common mistakes.
- The "when to use Kill Switch vs Circuit Breaker vs Rate Limiter" decision is not documented — three overlapping reliability filters with no guidance on selection.

#### Wolverine

**Documentation structure:**

- Guide page: `wolverinefx.net/guide/handlers/middleware`
- Tutorial: `wolverinefx.net/tutorials/middleware`

**Key differences from Mocha:**

Wolverine takes a fundamentally different architectural approach to middleware: **source code generation**. At startup, Wolverine uses `JasperFx.CodeGeneration` to generate and compile C# code specific to each handler. The middleware is "woven into" the handler rather than wrapping it at runtime via delegate chains.

The practical consequence is that the documentation can show you the _generated output_ of your middleware:

```csharp
// What Wolverine generates for a handler with StopwatchFrame middleware:
public class HandleMessage_Generated
{
    public async Task HandleAsync(IMessageContext context, MyMessage message)
    {
        var stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();
        try
        {
            await Inner.HandleAsync(context, message);
        }
        finally
        {
            stopwatch.Stop();
            // record timing
        }
    }
}
```

This transparency is pedagogically powerful — the developer can see exactly what the framework produces, which removes the "magic" complaint common with middleware patterns.

Wolverine's convention-based middleware (methods named `Before`, `After`, `Finally`) eliminates the factory pattern entirely. A middleware class is just a class with appropriately-named methods. This is the most frictionless authoring experience of all three competitors.

The tutorial teaches through problem → solution progression: shows a handler that repeatedly loads an Account from a database, extracts that into a `LoadAccountMiddleware`, and demonstrates how the loaded `Account` flows into the handler via return-value injection. This is realistic and illustrative.

**What they do well:**

- Code generation transparency is a unique competitive advantage in documentation terms: showing generated output eliminates all ambiguity about how the pipeline executes.
- Convention-based authoring (method naming) requires the least boilerplate.
- Return-value injection (middleware returning a value that flows into the handler as a parameter) is documented with a complete end-to-end example.
- `HandlerContinuation.Stop` for short-circuiting is explicit and testable.
- Policy-based selective application (`AddMiddleware(typeof(...), x => x.MessageType.CanBeCastTo<IAccountCommand>())`) documents applying middleware to subsets of handlers, not all handlers globally.

**What they do poorly:**

- The code generation model creates a conceptual barrier for developers coming from ASP.NET Core. The "before/after next()" mental model does not directly apply.
- Startup time implications of code generation are not addressed.
- No diagram showing how the generated code relates to the original handler.
- The three-tier implementation options (conventional, attribute, Frame-based) are not clearly ranked by complexity, leading to analysis paralysis for new users.

---

### Best Practices Found

**1. The "nesting" diagram is essential, not optional.**
Every high-quality middleware documentation page uses some visual showing that middleware A wraps middleware B which wraps C. ASP.NET Core's documentation uses an image showing arrows going in through A, B, C and returning through C, B, A. Mocha's ASCII diagram shows a vertical stack of pipeline stages but does not show the nesting/wrapping model within a single pipeline. This is the most important diagram to add.

**2. Anchor to a named pattern early.**
MassTransit explicitly names the "Pipes and Filters" pattern from _Enterprise Integration Patterns_ (Hohpe & Woolf). NServiceBus connects to "ASP.NET Core middleware." Mocha's page makes the ASP.NET Core analogy but does not link to the relevant Microsoft documentation or name the underlying pattern. Naming the pattern gives developers a vocabulary to search for additional resources independently.

**3. Show the scoped DI lifetime issue explicitly.**
NServiceBus and Wolverine both document that middleware dependencies behave as singletons within the pipeline compilation. Mocha resolves this via the factory pattern (`context.Services.GetRequiredService<T>()` in the factory lambda), but the page does not explain _why_ this pattern exists. Without this explanation, developers will write:

```csharp
// Wrong: ScopedService captured as singleton
var svc = serviceProvider.GetRequiredService<IMyScopedService>();
return ctx => svc.DoSomething(ctx, next);
```

This is a latent bug that the factory pattern prevents, but only if the developer understands why.

**4. Provide the troubleshooting section before the reference.**
Mocha puts troubleshooting at the end (after built-in reference tables). NServiceBus embeds warnings inline at the point of the API they apply to. The most common mistakes (wrong registration timing, wrong ordering method, key not found) should appear adjacent to the relevant registration API, not 300 lines later.

**5. Real-world motivating examples outperform toy examples.**
NServiceBus's "Unit of Work" and "Message Signing" samples are more persuasive than logging or timing examples because they demonstrate problems developers actually face. Mocha's three examples (logging, tenant headers, timing) are adequate but could be strengthened with a database-transaction-scope example for the consumer pipeline, since that is the most common real use case.

**6. Integrate pipeline concepts into adjacent pages, not just this page.**
NServiceBus references behaviors throughout its reliability, sagas, and outbox documentation. MassTransit's filter documentation links from its circuit-breaker configuration. Mocha's Reliability page presumably describes the circuit breaker behavior but does not say "this is the CircuitBreaker middleware in the receive pipeline." Readers of Reliability should be able to trace back to the middleware that implements it.

**7. Show a "where does my middleware fit" decision tree or table.**
Wolverine's documentation helps developers decide whether to use conventional middleware, attributes, or frame-based code generation. Mocha could add guidance on which pipeline (dispatch vs. receive vs. consumer) to target for common use cases: tenant context → dispatch, database transactions → consumer, rate limiting → receive.

---

### External References

- **Pipes and Filters Pattern (EIP):** https://www.enterpriseintegrationpatterns.com/patterns/messaging/PipesAndFilters.html — The canonical source defining the pattern that both MassTransit and Mocha implement. Citing this in the Mocha docs would give developers a shared vocabulary.

- **ASP.NET Core Middleware (Microsoft Learn):** https://learn.microsoft.com/en-us/aspnet/core/fundamentals/middleware/?view=aspnetcore-10.0 — Contains the definitive "before and after next()" diagram that Mocha references but does not reproduce. Linking to it would anchor the analogy.

- **NServiceBus Pipeline Overview:** https://docs.particular.net/nservicebus/pipeline/ — The most thorough competing treatment of this topic, with Mermaid diagrams on the steps/stages page.

- **NServiceBus Behaviors:** https://docs.particular.net/nservicebus/pipeline/manipulate-with-behaviors — Best-in-class example of the singleton dependency warning that Mocha's documentation omits.

- **NServiceBus Unit of Work Sample:** https://docs.particular.net/samples/pipeline/unit-of-work/ — Production-realistic pipeline extension example (database transaction scope).

- **MassTransit Middleware:** https://masstransit.io/documentation/configuration/middleware — Demonstrates the `IFilter<T>` interface contract approach and the Probe() introspection capability.

- **MassTransit Middleware Filters:** https://masstransit.io/documentation/configuration/middleware/filters — Built-in reliability filter documentation organized as a structured reference with configuration tables.

- **Wolverine Middleware Guide:** https://wolverinefx.net/guide/handlers/middleware — Unique code-generation transparency model; shows generated output of middleware composition.

- **Wolverine Custom Middleware Tutorial:** https://wolverinefx.net/tutorials/middleware — Best example of problem → solution teaching progression in competing .NET messaging docs.

- **NimblePros: NServiceBus Behaviors as Messaging Middleware:** https://blog.nimblepros.com/blogs/messaging-middleware-nservicebus-behaviors/ — Community article with correlation-ID example and unit testing guidance for behaviors.

- **Andrew Lock: Understanding Your Middleware Pipeline in .NET 6:** https://andrewlock.net/understanding-your-middleware-pipeline-in-dotnet-6-with-the-middleware-analysis-package/ — Deep-dive on ASP.NET Core pipeline inspection tooling; relevant for Mocha diagnostics.

---

### Recommendations: Standalone Page vs. Woven Throughout

**The fundamental structural problem:** The current page treats middleware as a self-contained advanced topic ("most of the time, the defaults work and you never configure middleware directly"). This framing is accurate but leads to a documentation structure where the Reliability page describes circuit breaking without saying "circuit breaking is implemented as the CircuitBreaker middleware," the Observability page describes telemetry without saying "telemetry is injected by the ReceiveInstrumentation middleware," and the Routing page describes message dispatch without connecting it to the Routing middleware.

**Recommendation 1: Add cross-references from every feature page back to this pipeline page.**
Each built-in middleware documented here should have a reciprocal link from its corresponding feature page. The Reliability page should have a sidebar note: "The CircuitBreaker and DeadLetter behaviors described here are implemented as middleware in the receive pipeline. See Middleware and Pipelines for positioning and customization."

**Recommendation 2: Add the nesting/wrapping diagram within a single pipeline.**
The existing ASCII diagram is valuable for showing the three-stage architecture. Add a second diagram showing the nesting model for a single pipeline stage — specifically the receive pipeline — showing how each middleware wraps the next, and how exceptions propagate back out:

```
Receive message
→ TransportCircuitBreaker.InvokeAsync
  → ConcurrencyLimiter.InvokeAsync
    → Instrumentation.InvokeAsync
      → [YOUR MIDDLEWARE here]
        → DeadLetter.InvokeAsync
          → Fault.InvokeAsync
            → CircuitBreaker.InvokeAsync
              → Expiry.InvokeAsync
                → MessageTypeSelection.InvokeAsync
                  → Routing.InvokeAsync
                    → [handler called]
                  ← return
                ← return
              ← return
            ← exception caught here ←←
          ← forwarded to error endpoint ←←
        ← return
```

**Recommendation 3: Explain the factory pattern's purpose before showing its implementation.**
The current page shows the `Create()` static factory before explaining why it exists. Move the explanation of DI lifetime (from the compiler section) to just before the first middleware example. One paragraph noting "The factory lambda runs once per message and resolves services fresh from the request-scoped container. If you captured an `IMyService` outside the lambda, it would be shared across all messages and behave as a singleton, which breaks scoped services like `DbContext`."

**Recommendation 4: Add a "which pipeline should I target?" guide.**
A short table or decision tree:

| Use case                                           | Pipeline                             |
| -------------------------------------------------- | ------------------------------------ |
| Add a header to every outgoing message             | Dispatch                             |
| Validate messages before sending                   | Dispatch                             |
| Rate-limit incoming messages at the endpoint level | Receive                              |
| Wrap every handler in a database transaction       | Consumer                             |
| Extract a tenant context from message headers      | Receive (before Routing) or Consumer |
| Time individual handler execution                  | Consumer                             |

**Recommendation 5: Add a Probe/diagnostics concept.**
MassTransit's `Probe()` method on filters is genuinely useful — it enables diagnostic tools to inspect the pipeline configuration at runtime. Mocha should document how to inspect the compiled pipeline (if it has this capability), or note it as a planned feature. The absence of diagnostic capability for pipelines is a gap compared to MassTransit.

**Recommendation 6: Add a real-world consumer pipeline example.**
Replace or supplement the `TimingConsumerMiddleware` example with a database unit-of-work example:

```csharp
internal sealed class UnitOfWorkConsumerMiddleware
{
    public async ValueTask InvokeAsync(IConsumeContext context, ConsumerDelegate next)
    {
        var db = context.Services.GetRequiredService<AppDbContext>();
        await using var tx = await db.Database.BeginTransactionAsync();
        try
        {
            await next(context);
            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }
    // ...
}
```

This demonstrates the most common real use case and shows why the try/catch around `next` matters for reliability.
