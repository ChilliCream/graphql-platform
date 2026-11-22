---
title: "Mocha vs. Wolverine"
metaTitle: "Mocha vs Wolverine: Build-Time Messaging for .NET"
description: "Compare Mocha and Wolverine (WolverineFx) for messaging in .NET: handler discovery, code generation, Native AOT, request/reply, the outbox, and sagas."
featuredImage: "mocha-vs-wolverine.png"
date: "2026-10-29"
tags: ["mocha", "comparison", "wolverine"]
author: Pascal Senn
authorUrl: https://github.com/pascalsenn
authorImageUrl: https://avatars.githubusercontent.com/u/14233220?v=4
about:
  - name: "Mocha"
    url: "/products/mocha"
  - name: "Wolverine"
    url: "https://wolverinefx.net/"
faq:
  - question: "Is Mocha free for commercial use?"
    answer: "Yes. Mocha is MIT licensed. Any company can use it in production for free, with no license key and no revenue limit."
  - question: "Does Mocha compile code at runtime?"
    answer: "No. The Mocha.Analyzers source generator writes the registration and dispatch code when you compile, so there is no Roslyn compiler at runtime and no generated code to check in or regenerate."
  - question: "Does Mocha support Native AOT?"
    answer: "Yes. Handlers are registered at build time, and for a service published with Native AOT the source generator writes the serialization code from the JsonSerializerContext you reference in the MessagingModule attribute. There is no separate code generation step."
  - question: "How does Mocha find my handlers?"
    answer: "A handler implements an interface such as IEventHandler<T> or IEventRequestHandler<TRequest, TResponse>, and the source generator registers it when you compile. Two handlers for the same command or request are a build error."
  - question: "Does Mocha have sagas and a transactional outbox?"
    answer: "Yes. Mocha has state machine sagas with timeouts, and a transactional outbox and inbox that live in your Entity Framework Core DbContext."
cta:
  title: "Try Mocha in your project."
  text: "Mocha is free for every company under the MIT license. Build your first service with the quick start, or move from Wolverine step by step with the migration guide. Moving a large system?"
  primary:
    label: "Get started with Mocha"
    href: "/docs/mocha/quick-start"
  secondary:
    label: "Migrate from Wolverine"
    href: "/docs/mocha/migration/coming-from-wolverine"
  contact: "Talk to us about your migration"
---

Building a message-driven application in .NET involves decisions that will shape your codebase for years. How are handlers discovered? When does code generation happen? How do you keep database changes and outgoing messages consistent? And how much of your messaging infrastructure should the compiler be able to verify?

These questions lead to an interesting design choice: **Should a messaging framework use conventions to keep your code short, or explicit contracts to make its behavior visible and verifiable?** Mocha and [Wolverine](https://wolverinefx.net/) approach this question from different directions.

Mocha favors explicit contracts. Handlers declare which messages they process through interfaces, requests specify their expected response types, and a source generator handles registration during the build. There are a few more declarations to write, but the compiler can verify relationships that would otherwise depend on framework conventions.

Wolverine embraces conventions. It recognizes handlers through method names and signatures, uses return types to determine what happens to messages, and generates the code needed to connect everything. The result is a programming model where handlers can be remarkably concise. You write the business logic, follow the framework's conventions, and let Wolverine take care of the rest. Its own [documentation](https://wolverinefx.net/guide/handlers/) is open about the trade: the goal is to remove code ceremony, "but that comes at the cost of using conventions that some developers will decry as 'too much magic.'"

Neither approach is accidental. They represent different ideas about what makes a framework productive. That distinction is becoming especially relevant as coding agents take on more of the work of writing, reading, and modifying applications. Generating a few extra lines of code is rarely the difficult part. Understanding what existing code does, identifying the consequences of a change, and verifying that everything still fits together are often more interesting challenges. Explicit contracts give both developers and coding agents something concrete to work with. That was one of our design principles for Mocha. A little boilerplate is worth it when it makes the code straightforward to read and change.

Both frameworks are MIT licensed, and both support familiar messaging patterns such as publish/subscribe, request/reply, transactional outboxes, and sagas. So this comparison isn't about whether one framework can do messaging and the other can't. It's about the experience of building and maintaining an application with each.

**Do you prefer a framework that uses conventions to reduce the code you write, or one that makes you declare more of your intentions so the compiler can check them?**

Let's look at how that choice plays out in practice.

# Handlers: Conventions vs. Contracts

Every messaging framework needs a way to connect incoming messages to the code that handles them. There are two natural approaches. A framework can recognize handlers based on conventions, or it can ask developers to declare the relationship explicitly. Wolverine chooses the first approach. Mocha chooses the second.

Both can produce clean, readable handlers. The difference becomes more interesting when you start changing those handlers, refactoring your application, or trying to understand how everything fits together. In Mocha, handlers implement interfaces that describe their responsibilities:

- `IEventHandler<T>` handles published events.
- `IEventRequestHandler<T>` handles commands.
- `IEventRequestHandler<TRequest, TResponse>` handles requests that expect a response.

These interfaces establish contracts the compiler can verify.

Wolverine takes a convention-based approach. It scans public classes with names ending in `Handler` or `Consumer` and looks for public methods named `Handle`, `Handles`, `Consume`, or `Consumes`, including their `Async` variants. You can also use a marker interface or attribute instead of the naming conventions, but discovery still follows Wolverine's rules.

As Wolverine's [handler discovery](https://wolverinefx.net/guide/handlers/discovery) documentation explains, "in all cases, Wolverine assumes that the first argument is the incoming message." This approach has an appealing quality: you can write an ordinary C# method and have Wolverine recognize it as a message handler. Here's what that looks like alongside Mocha.

<CodeComparison>

```csharp filename="Mocha" {3,5,6}
public class OrderPlacedHandler(
    IInvoiceService invoices)
    : IEventHandler<OrderPlaced>
{
    public async ValueTask HandleAsync(
        OrderPlaced message, CancellationToken ct)
        => await invoices.CreateAsync(
            message.OrderId, ct);
}
```

```csharp filename="Wolverine" {1,4,5}
public class OrderPlacedHandler(
    IInvoiceService invoices)
{
    public async Task Handle(
        OrderPlaced message, CancellationToken ct)
        => await invoices.CreateAsync(
            message.OrderId, ct);
}
```

</CodeComparison>

These examples are almost identical. Both receive an `OrderPlaced` event. Both call an invoice service. Both keep the business logic easy to find. Wolverine saves a little code because the handler doesn't need to implement an interface. The framework recognizes the class and method through its conventions. Mocha adds `IEventHandler<OrderPlaced>`, which might seem like a small detail. And in terms of lines of code, it is.

But that declaration changes what the compiler knows about your application.

## What happens when a handler changes?

Imagine someone is refactoring the ordering service. In Mocha, `IEventHandler<OrderPlaced>` tells the compiler that this class must implement a particular method with a particular message type and return type. If someone accidentally changes the method signature or removes the required implementation, the compiler reports an error. The interface also gives anyone reading the class an immediate answer to a useful question: which message is this handler responsible for?

Wolverine works differently. Because the handler relationship is established through conventions, the compiler doesn't know that `Handle` is supposed to be a message handler. Renaming it to something Wolverine doesn't recognize, making it private, or moving its class outside the assemblies Wolverine scans can change whether the framework discovers it. The C# code still compiles.

Wolverine's documentation discusses this possibility in its troubleshooting guidance, noting that "sometimes Wolverine isn't finding handler methods for some reason or another." That's a natural consequence of convention-based discovery. The framework, rather than the C# type system, determines whether a method participates in messaging.

And conventions aren't limited to handler discovery. Wolverine also recognizes methods named `Before`, `Load`, or `Validate` as code that runs before a handler. Methods named `After`, `PostProcess`, or `Finally` run afterward. These names are matched case sensitively, as described in its [middleware conventions](https://wolverinefx.net/guide/handlers/middleware).

Return values can also carry meaning. Unless Wolverine recognizes a returned type as something special, it treats that value as a cascading message to publish. When a handler returns a tuple, its elements are processed independently, as explained in the [return values](https://wolverinefx.net/guide/handlers/return-values) documentation.

These conventions are part of what makes Wolverine's programming model concise. A handler can express quite a bit of behavior without explicitly declaring every relationship. Once you know the conventions, the framework does a substantial amount of work for you. The tradeoff is that understanding a method's behavior sometimes requires knowing more than what its C# signature tells you.

## Why explicit contracts matter as applications grow

Consider what happens when an application grows from a handful of handlers to dozens. Developers start renaming methods, moving classes between projects, changing return types, and introducing new middleware. Coding agents may be making some of those changes, too. With convention-based discovery, a correct change needs to preserve both the C# code and the framework's expectations.

With interface-based discovery, more of those expectations are represented directly in the type system. That doesn't eliminate mistakes, but it gives the compiler more opportunities to identify them. It also makes the code easier to inspect. A developer or coding agent can see `IEventHandler<OrderPlaced>`, find the corresponding contract, and understand the relationship without first reconstructing the framework's discovery rules.

Wolverine's author discussed this tradeoff in 2023, acknowledging that there's "admittedly some room for confusion, especially for newer users," and describing the effort to make conventions "easy to understand and predictable" as "a work in progress" ([Unraveling the Magic in Wolverine](https://jeremydmiller.com/2023/10/02/unraveling-the-magic-in-wolverine/)).

Wolverine provides tooling to inspect generated code and understand what happens behind the scenes. That's useful when working with a convention-based model. Mocha approaches the same problem from another direction. Rather than relying on developers to recognize the conventions, **Mocha makes the handler relationship an explicit part of the code and lets the compiler verify it.**

The additional interface declaration is small. The benefit is that the relationship remains visible and checked as the application evolves.

# Code Generation: Build Time vs. Runtime

Both Mocha and Wolverine use code generation to connect handlers, messages, and middleware. That's an important similarity. Neither framework expects you to manually write all the infrastructure needed to execute a message handler efficiently. The difference is when generation happens and how the generated code becomes part of your application.

Mocha generates code during the normal .NET build. Wolverine generates and compiles code at runtime by default, while also supporting a workflow where generated code is written ahead of deployment. Both approaches are designed to produce the execution code the framework needs. They simply make different choices about when that work should happen.

## Mocha generates code as part of the build

Mocha uses a Roslyn source generator provided by `Mocha.Analyzers`. When you build your application, the generator discovers handlers through their interfaces, validates their contracts, and produces the registration code that connects them to the messaging infrastructure. Suppose your handlers live in `MyApp.Ordering`. Mocha generates an `AddOrdering()` method that registers those handlers. You can then use that method in your application configuration.

Here's how the setup compares with Wolverine's static code-generation workflow.

<CodeComparison>

```csharp filename="Mocha"
builder.Services
    .AddMessageBus()
    .AddOrdering() // written at build time
    .AddRabbitMQ();
```

```csharp filename="Wolverine 6"
builder.Host.UseWolverine(opts =>
{
    opts.UseRabbitMq()
        .AutoProvision()
        .UseConventionalRouting();

    // Production: load the files that
    // `dotnet run -- codegen write` wrote
    opts.CodeGeneration.TypeLoadMode =
        TypeLoadMode.Static;
});
```

</CodeComparison>

With Mocha, generated registration code is part of the ordinary build process. You don't need to run a separate generation command or maintain generated source files. The generator runs again when you rebuild, so changes to handlers are reflected in the generated registration code. The build can also catch certain messaging configuration problems. For example, registering two handlers for the same command or request produces a build error.

That's a useful property because it moves feedback into a workflow every .NET developer already uses: compile the application and see whether its contracts are valid. The generated code is then compiled into the application assembly, ready for execution.

## How Wolverine handles generated code

Wolverine also generates optimized execution code for handlers. Its default approach is to generate and compile handler wrappers at runtime using Roslyn, when they're first needed. Starting with Wolverine 6.0, runtime compilation requires the separate `WolverineFx.RuntimeCompilation` package.

This approach fits Wolverine's convention-based design. The framework discovers handlers and generates the code needed to execute them without requiring a separate generation command during development. There's a cost to that flexibility, though. Runtime compilation requires compiler availability after deployment and introduces additional memory usage and startup costs.

For production, Wolverine's [code generation documentation](https://wolverinefx.net/guide/codegen) recommends a different setup: you run `codegen write` ahead of time and configure `TypeLoadMode.Static` so Wolverine loads the generated types instead of compiling them at runtime. That gives you an application that no longer needs runtime code compilation.

The tradeoff is that generated code now has its own lifecycle. When handlers or middleware change, the generated files may need to be updated. That can be handled manually or incorporated into a CI pipeline, but it's a separate responsibility from compiling the application.

Wolverine's documentation also calls out an important detail. It warns that you "may need to delete the existing source code when you change handler signatures or add or remove middleware," because "Nothing in Wolverine is able to detect that the generated source code needs to be rewritten." In `Static` mode, Wolverine checks at startup whether the pre-generated handler types exist. That check happens when the application starts rather than during compilation.

This is where the two development workflows feel different. Wolverine gives you the choice between runtime compilation and a separate static-generation process. Mocha makes build-time generation the default and only workflow. Change a handler, rebuild the project, and the generated registration code is recreated as part of that build.

**The generated code follows the source code through the normal compilation process, without a separate synchronization step.**

For teams that want compilation to be the point where application contracts and generated infrastructure come together, that's a meaningful advantage.

## What about Native AOT?

Native AOT makes the timing of code generation particularly important. Because Native AOT doesn't support compiling arbitrary new code at runtime, messaging frameworks need their generated execution code to be ready before the application starts. Mocha already follows that model.

Its handlers and registration code are generated during the build and compiled into the application. For Native AOT deployments, Mocha also generates serialization support based on a `JsonSerializerContext`. Wolverine supports Native AOT as well, using its static code-generation mode. Runtime compilation isn't an option in that environment. So both frameworks can produce applications that don't generate handler code at runtime. The distinction is in the workflow.

**Mocha uses build-time generation for ordinary development and Native AOT alike. Wolverine supports static generation, but requires you to choose and manage that workflow.**

If you prefer having one code-generation process that runs automatically with every build, Mocha's approach is particularly attractive.

# Request/Reply: Let the Compiler Check the Response

Request/reply is a common pattern in message-driven applications. Imagine an ordering service that needs to check the status of an order. It sends a `CheckOrderStatus` request and expects an `OrderStatus` response. Both Mocha and Wolverine support this interaction, including requests that travel between services through a message broker. The interesting difference is how they represent the relationship between the request and its response.

In Mocha, the request itself declares the expected response type. In Wolverine, the caller specifies it. Both approaches work, but they give the compiler different amounts of information.

<MochaVisual
  name="order-status"
  caption="The ordering service sends a typed request and awaits the response from the catalog service."
/>

## Mocha connects the request to its response type

In Mocha, a request implements an interface that defines its expected response.

```csharp
public record CheckOrderStatus(Guid OrderId)
    : IEventRequest<OrderStatus>;
```

This establishes a contract between `CheckOrderStatus` and `OrderStatus`. Anyone reading the request can see what it returns. More importantly, the compiler can use that relationship when the request is handled or sent. Let's compare the complete implementations.

<CodeComparison>

```csharp filename="Mocha"
public record CheckOrderStatus(Guid OrderId)
    : IEventRequest<OrderStatus>;

public class CheckOrderStatusHandler(
    OrderStore orders)
    : IEventRequestHandler<
        CheckOrderStatus, OrderStatus>
{
    public async ValueTask<OrderStatus> HandleAsync(
        CheckOrderStatus request,
        CancellationToken ct)
        => await orders.GetStatusAsync(
            request.OrderId);
}

// Caller
var status = await bus.RequestAsync(
    new CheckOrderStatus(orderId), ct);
```

```csharp filename="Wolverine"
public record CheckOrderStatus(Guid OrderId);

public class CheckOrderStatusHandler(
    OrderStore orders)
{
    public Task<OrderStatus> Handle(
        CheckOrderStatus request)
        => orders.GetStatusAsync(
            request.OrderId);
}

// Caller: the response type is not checked
var status = await bus.InvokeAsync<OrderStatus>(
    new CheckOrderStatus(orderId), ct);
```

</CodeComparison>

Both examples are straightforward. Wolverine keeps the request record simple. It doesn't need to implement an interface or declare a response type. The caller supplies `OrderStatus` when invoking the request. Mocha puts that information in the request declaration instead. As a result, the caller only needs to provide the request. The compiler already knows which response type to expect. Why might that matter? Consider this Wolverine call:

```csharp
var status = await bus.InvokeAsync<Customer>(
    new CheckOrderStatus(orderId), ct);
```

The caller asks for a `Customer`, even though the handler returns an `OrderStatus`. Nothing in the request's type definition tells the compiler that the combination is incorrect. The code compiles because the expected response type is supplied independently by the caller. With Mocha, that relationship is established through `IEventRequest<OrderStatus>`. The caller cannot independently choose an unrelated response type for the same request.

This becomes more useful as requests are shared across an application. Imagine `CheckOrderStatus` being used by several services and called from many different places. If the response contract changes, you want affected code to be easy to identify. With Mocha, the response type is part of the request's definition, so the compiler can help reveal incompatible usages.

With Wolverine, callers specify the response type themselves, which means those declarations need to remain consistent with the handlers. Neither approach requires much code. But one makes the relationship a compiler-checked contract, while the other relies on the caller knowing which response to request.

**Mocha's advantage is that the request and its response type remain connected wherever the request is used.**

## Remote requests still need care

A compiler-checked response type doesn't eliminate the challenges of distributed communication. A remote service can be unavailable. A connection can fail. A request can time out. And two services can agree perfectly on a message contract while still being unable to communicate. Wolverine uses a default timeout of five seconds for remote invocations, and its maintainers recommend being cautious about remote request/reply. The same architectural considerations apply to Mocha.

When one service sends a request and waits for another service to respond, the caller depends on the availability and responsiveness of the receiving service. That temporal coupling is sometimes appropriate. Other times, asynchronous messaging may be a better fit. The important thing is to make that decision deliberately.

Mocha doesn't make remote requests inherently more reliable. What it does provide is a stronger type-level relationship between a request and its expected response. That removes one category of mismatch from an interaction that already has enough distributed-system concerns to deal with.

# Reliable Messaging: Keeping the Database and Broker in Sync

Let's move from handler contracts to a problem that affects almost every message-driven application: keeping database changes and published messages consistent. Imagine a customer placing an order. Your application needs to save the order in PostgreSQL and publish an `OrderPlaced` event so other services can react. The obvious implementation is to save the order and then publish the event. But what happens if the database transaction succeeds and publishing fails?

The order exists, yet other services never learn about it. Now reverse the situation. The event is published, but the database transaction rolls back. Other services may begin processing an order that doesn't exist. This is the problem transactional outboxes are designed to solve. Instead of immediately publishing the event to the broker, the application stores the outgoing message in the same database transaction as its business changes.

Once that transaction commits, a background process delivers the message. The order and the intent to publish `OrderPlaced` are therefore committed together. The inbox addresses a related concern on the receiving side. Message brokers can deliver messages more than once. An inbox records which messages have already been processed so the application can recognize duplicates.

<MochaVisual
  name="outbox"
  caption="The outbox commits outgoing messages with your database changes, and the inbox skips a message the broker delivers twice."
/>

Both Mocha and Wolverine support transactional outboxes and inboxes. Both provide the reliability mechanisms needed to coordinate database changes and messaging. Where they differ is in how those mechanisms are configured.

## Mocha keeps persistence configuration together

Mocha integrates its outbox and inbox with an Entity Framework Core `DbContext`. The outbox, inbox, and transactional handling can be configured together. Wolverine provides the corresponding capabilities through its database integration and messaging policies. Here's how the two configurations compare.

<CodeComparison>

```csharp filename="Mocha"
.AddEntityFramework<AppDbContext>(p =>
{
    p.UsePostgresOutbox();
    p.UseTransaction();
    p.UsePostgresInbox();
})
```

```csharp filename="Wolverine"
opts.Services
    .AddDbContextWithWolverineIntegration<
        AppDbContext>(x => x.UseNpgsql(connection));
opts.PersistMessagesWithPostgresql(
    connection, "wolverine");
opts.UseEntityFrameworkCoreTransactions();
opts.Policies.AutoApplyTransactions();
opts.Policies.UseDurableLocalQueues();
opts.Policies
    .UseDurableOutboxOnAllSendingEndpoints();
opts.Policies.UseDurableInboxOnAllListeners();
```

</CodeComparison>

The difference here is less about compiler checking and more about how related configuration is organized. In Mocha, the persistence configuration is grouped around `AppDbContext`. You can see the PostgreSQL outbox, transaction handling, and PostgreSQL inbox in one place. Wolverine expresses these concerns through several configuration APIs.

Its setup includes Entity Framework Core integration, PostgreSQL message persistence, transaction policies, durable local queues, and policies for sending endpoints and listeners. That gives you a configuration model built around Wolverine's broader messaging infrastructure. Mocha takes a more focused approach by keeping the related persistence settings together.

For an application already using Entity Framework Core, this makes it particularly easy to see how the messaging infrastructure participates in database transactions. It also makes reviewing the configuration simpler. When someone needs to understand whether an application uses an outbox, an inbox, and transactional handling, the relevant Mocha settings are close together. With Wolverine, they need to understand how the different policies contribute to the overall behavior.

Both approaches can provide reliable messaging. **Mocha's advantage is the way it brings the related configuration into one cohesive definition.**

It's worth remembering that these reliability patterns have limits regardless of the framework. An outbox doesn't guarantee that every message will be delivered exactly once. Messages can still be redelivered, and consumers can fail during processing. That's why the inbox is an important companion to the outbox. Together, they help applications remain consistent even when databases, brokers, and consumers don't all succeed at the same time. The underlying patterns are familiar in both frameworks. The choice is largely about how you prefer to configure and reason about them.

# Sagas: Making Long-Running Workflows Explicit

Not every business process fits into a single request or database transaction. Consider an order moving through a system. It might start as submitted, then be validated, accepted, paid for, and eventually shipped. Different services may own different steps. Some operations finish immediately, while others take minutes or hours. And throughout the process, things can go wrong. A payment may fail. An order may be rejected. A shipment may be delayed.

A saga helps coordinate these long-running workflows by maintaining state and responding to events as the process progresses. Both Mocha and Wolverine support sagas. But they use different models to describe them.

**Mocha represents a saga as an explicit state machine. Wolverine represents it as a class whose methods respond to messages through conventions.**

This is another example of the central design choice between the frameworks. Wolverine lets you express workflow behavior through ordinary methods. Mocha asks you to define the states and transitions that make up the workflow. Let's look at a simple example. An `OrderSubmitted` event starts the saga. Later, an `OrderAccepted` event moves it forward.

<MochaVisual
  name="order-saga"
  caption="A saga moves through its states as messages arrive and keeps its state in between."
/>

## Explicit states and transitions in Mocha

Mocha lets you define the workflow in terms of states, events, and transitions. You describe how the saga begins, which events are accepted in each state, and where those events lead. Here's the order workflow in both frameworks.

<CodeComparison>

```csharp filename="Mocha"
public sealed class OrderSaga : Saga<OrderState>
{
    private const string Submitted = nameof(Submitted);
    private const string Accepted = nameof(Accepted);

    protected override void Configure(
        ISagaDescriptor<OrderState> saga)
    {
        saga.Initially()
            .OnEvent<OrderSubmitted>()
            .StateFactory(e =>
                new OrderState { Id = e.OrderId })
            .TransitionTo(Submitted);

        saga.During(Submitted)
            .OnEvent<OrderAccepted>()
            .TransitionTo(Accepted);

        saga.Finally(Accepted);
    }
}
```

```csharp filename="Wolverine"
public record OrderSubmitted(Guid OrderId);
public record OrderAccepted(
    [property: SagaIdentity] Guid OrderId);

public class OrderSaga : Saga
{
    public Guid Id { get; set; }

    public static OrderSaga Start(
        OrderSubmitted submitted)
        => new() { Id = submitted.OrderId };

    public void Handle(OrderAccepted accepted)
        => MarkCompleted();
}
```

</CodeComparison>

Both implementations describe the same basic process. And Wolverine's version is noticeably shorter. The static `Start` method creates the saga when `OrderSubmitted` arrives. The `Handle` method reacts to `OrderAccepted` and completes the workflow. For a simple saga, that's a compact way to express the behavior. Mocha uses more code because it describes the workflow differently.

Rather than simply implementing what should happen when each message arrives, it explicitly defines the relationship between messages and states. The saga starts by accepting `OrderSubmitted`. That event creates an `OrderState` and moves the saga into the `Submitted` state. While the saga is in `Submitted`, it accepts `OrderAccepted` and transitions to `Accepted`. Finally, `Accepted` marks the end of the workflow.

The important distinction is that the workflow's structure is visible directly in its definition. You can read through the states and transitions to understand how the process progresses.

## Wolverine uses methods and conventions

Wolverine's saga model follows the same philosophy as its handlers. A static `Start` method creates the saga. Methods named `Handle` process subsequent messages. Calling `MarkCompleted()` indicates that the workflow has finished. The saga's properties represent its state, and its methods contain the logic that responds to events. This keeps saga classes close to ordinary C# code. You don't have to introduce a separate state-machine definition to implement a simple workflow.

The tradeoff is that the workflow isn't described as an explicit collection of named states and transitions. Instead, its behavior emerges from the methods and their logic. For straightforward processes, that may be all you need. For more involved workflows, understanding which events are meaningful at each stage requires following the code.

Mocha makes a different tradeoff. It asks you to write the state-machine definition up front, but that definition becomes a central place to understand the workflow. You can examine the transitions without first tracing the implementation of every event handler. That becomes useful when investigating unexpected behavior, reviewing changes, or explaining a business process to someone new to the application.

## When workflows become more complicated

For a two-step workflow, the difference may not seem particularly important. Wolverine's concise implementation is easy to understand, and the explicit state machine requires additional declarations. But long-running workflows have a tendency to grow. An order might be rejected before payment. A payment might fail and need compensation. A shipment might be delayed. A timeout might trigger a different path through the process.

As those possibilities accumulate, understanding the workflow becomes more challenging. With Wolverine, that means following the methods that respond to messages and examining the logic that determines what happens next. The saga's properties hold its state, while the handlers define its behavior. With Mocha, the states and transitions remain part of an explicit workflow definition. You can see which events are accepted during a particular state and where those events lead.

That makes the workflow easier to inspect as a whole. It also helps when reviewing changes. If someone introduces a new event or transition, the change appears in the state-machine definition alongside the existing workflow. A developer or coding agent examining the saga has a clearly defined structure to follow rather than having to reconstruct the complete process from individual methods.

**Wolverine optimizes for expressing saga behavior with less ceremony. Mocha optimizes for making the workflow's structure explicit.**

Which approach you prefer may depend on the complexity of your workflows. Mocha's advantage becomes especially clear when sagas contain many states and possible transitions.

## How messages find their saga

A saga also needs a way to associate incoming messages with the correct running instance. Imagine thousands of orders moving through your system simultaneously. When an `OrderAccepted` event arrives, the framework needs to find the saga responsible for that particular order. This is called correlation. Mocha makes correlation part of the message contract. In our example, the state factory initializes the saga ID from `OrderSubmitted.OrderId`.

Subsequent events implement `ICorrelatable` and expose the relevant identifier through `CorrelationId`. That makes the correlation relationship explicit in the message types. Wolverine uses conventions and attributes. It looks for members named `OrderSagaId`, `SagaId`, or `Id`, or for a property decorated with `[SagaIdentity]`.

Wolverine also recognizes method names such as `Start`, `Orchestrate`, and `Handle`, along with their variants, to determine which methods create or continue a saga. This fits naturally with its convention-based programming model. Mocha instead puts more of those relationships into interfaces and explicit declarations.

Once again, the choice is between reducing the amount of code required to express a relationship and making that relationship directly visible in the type system.

## More than just state transitions

Real sagas do more than move between states. They may publish events, send commands to other services, schedule timeouts, or coordinate request/reply interactions. Mocha supports these patterns. A saga can even begin with a request and return a response once the workflow has finished. Saga state can be persisted in PostgreSQL through Entity Framework Core or kept in memory during testing. There is one behavior worth understanding when designing a Mocha saga.

**If a correlated event arrives before its saga instance exists, Mocha ignores it.**

Imagine an `OrderSubmitted` event starting a saga and triggering another service to process the order. If that service responds before the saga state has been persisted, the response may arrive too early. The way to avoid this is to publish downstream messages as part of the initial transition and commit them together with the saga state through the outbox. That ensures other services don't begin processing the work before the saga is ready to receive their responses.

It's a useful reminder that explicit workflow modeling and reliable messaging need to work together. A state machine makes the intended process visible. Transactional messaging helps ensure that the process behaves consistently when multiple services are involved.

# Moving an Existing Wolverine Application to Mocha

If you're already using Wolverine, moving to Mocha doesn't mean abandoning the messaging concepts your application is built around. You'll still have handlers processing messages, requests producing responses, outboxes protecting database changes, and sagas coordinating long-running workflows. Those concepts remain familiar. What changes is how you express them.

Wolverine applications rely on conventions for many of these relationships. In Mocha, you'll replace those conventions with handler interfaces, typed request contracts, build-time registration, and explicit saga state machines. There is also an important compatibility consideration.

**Mocha and Wolverine use different message formats.**

That means migration involves more than replacing packages and updating handler methods. If services communicate through a broker, you can't assume that a Wolverine service and a Mocha service will automatically understand each other's existing transport messages. It's generally better to plan migration around related message flows and move communicating services together where possible. A typical migration looks like this:

1. **Install Mocha.** Add the `Mocha`, `Mocha.Analyzers`, and relevant transport packages to your application.
2. **Replace the bus configuration.** Swap `UseWolverine` for `AddMessageBus()`, the generated registration method, and your transport configuration.
3. **Update your handlers.** Implement the appropriate Mocha handler interfaces and rename `Handle` methods to `HandleAsync`.
4. **Migrate request/reply calls.** Move in-process `InvokeAsync` calls to the Mocha mediator, and replace remote `InvokeAsync<T>` calls with `RequestAsync`.
5. **Move reliability and workflow configuration.** Translate your retry policies, outbox and inbox setup, and sagas to their Mocha equivalents.

You don't necessarily need to migrate your entire application at once. Related services can move to Mocha while other parts of the system continue using Wolverine, provided you account for message-format differences and communication boundaries.

The migration is also an opportunity to make existing relationships more explicit. Handler responsibilities become interfaces. Request and response relationships become part of message contracts. Saga workflows become state-machine definitions. For a more detailed walkthrough, including code examples, see the [migration guide from Wolverine to Mocha](../docs/mocha/migration/coming-from-wolverine.md).

<MigrationGuideCard href="/docs/mocha/migration/coming-from-wolverine" from="Wolverine" />

# Feature comparison

So far, we've focused on the programming models of Mocha and Wolverine. That's where many of their most interesting differences appear, but it's only part of the decision. Transport support, database integrations, reliability features, and framework compatibility matter just as much. In some cases, those requirements may determine your choice before you even get to handler conventions or code generation. Here's how their capabilities compare.

## Transports

Mocha supports RabbitMQ, Azure Service Bus, and in-memory messaging, with PostgreSQL transport support currently in preview. Wolverine currently covers more transports, including SQL Server, Amazon SQS/SNS, and Kafka.

| Feature           | Mocha                                                                                                  | Wolverine |
| ----------------- | ------------------------------------------------------------------------------------------------------ | --------- |
| RabbitMQ          | Yes                                                                                                    | Yes       |
| Azure Service Bus | Yes                                                                                                    | Yes       |
| PostgreSQL        | Preview                                                                                                | Yes       |
| SQL Server        | <FeatureStatus status="planned" href="https://github.com/ChilliCream/graphql-platform/issues/11031" /> | Yes       |
| Amazon SQS/SNS    | <FeatureStatus status="planned" href="https://github.com/ChilliCream/graphql-platform/issues/11032" /> | Yes       |
| Kafka             | <FeatureStatus status="planned" href="https://github.com/ChilliCream/graphql-platform/issues/11033" /> | Yes       |
| In-memory         | Yes                                                                                                    | Yes       |

If your application depends on a transport Mocha doesn't support yet, account for that before you migrate. For applications using RabbitMQ or Azure Service Bus, both frameworks support the transport layer, so the programming model becomes a more prominent part of the decision. If explicit contracts and build-time generation are priorities for your application, Mocha provides those capabilities alongside its supported transports.

## Messaging

Both frameworks support the core messaging patterns used in .NET applications. You can publish events, send commands, implement request/reply interactions, schedule messages, consume messages in batches, and coordinate workflows through sagas. The more interesting differences are in how those patterns are represented in code.

| Feature                        | Mocha          | Wolverine             |
| ------------------------------ | -------------- | --------------------- |
| Publish and send               | Yes            | Yes                   |
| Request/reply                  | Yes            | Yes                   |
| Compile-time checked responses | Yes            | No                    |
| Batch consumers                | Yes            | Yes                   |
| Scheduled messages             | Yes            | Yes                   |
| Sagas                          | State machines | Classes by convention |
| In-process mediator            | Yes            | Yes                   |

Wolverine's convention-based approach keeps handlers and saga implementations concise. Mocha uses explicit contracts to give the compiler more information about the relationships between messages, handlers, and responses. The same distinction appears in saga design. Wolverine expresses workflow behavior through methods. Mocha makes states and transitions part of the workflow definition. Both are legitimate ways to build message-driven applications.

The question is whether you value the reduced ceremony of conventions or the additional structure of explicit contracts.

## Reliability

Reliable messaging is important regardless of which programming model you prefer. Both frameworks support retries, redelivery, transactional outboxes and inboxes, circuit breakers, and concurrency controls.

| Feature                                | Mocha | Wolverine |
| -------------------------------------- | ----- | --------- |
| Retry and redelivery policies          | Yes   | Yes       |
| Circuit breaker and concurrency limits | Yes   | Yes       |
| Transactional outbox and inbox         | Yes   | Yes       |

The difference is primarily in configuration. Wolverine provides reliability capabilities through its messaging and persistence policies. Mocha groups related Entity Framework Core persistence settings together, making it easier to see how the outbox, inbox, and database transactions are configured. Both provide the underlying mechanisms. Mocha's approach may be preferable when you want those related settings to be visible together in one place.

## Storage

Persistent messaging needs storage for things such as outbox and inbox records, scheduled messages, and saga state. Mocha currently focuses on PostgreSQL. Wolverine supports additional database engines, including SQL Server and MySQL.

| Feature    | Mocha                                                                                                  | Wolverine |
| ---------- | ------------------------------------------------------------------------------------------------------ | --------- |
| PostgreSQL | Yes                                                                                                    | Yes       |
| SQL Server | <FeatureStatus status="planned" href="https://github.com/ChilliCream/graphql-platform/issues/11035" /> | Yes       |
| MySQL      | <FeatureStatus status="planned" href="https://github.com/ChilliCream/graphql-platform/issues/11036" /> | Yes       |

For applications already using PostgreSQL, Mocha provides the persistence integration needed for transactional messaging and durable saga state. Support for additional storage engines is planned.

## Development Experience

Feature tables are useful for determining whether a framework supports your infrastructure. They tell you less about what it feels like to work with that framework every day. That's where Mocha and Wolverine make some of their clearest design choices.

Mocha uses interfaces and explicit contracts, with source generation integrated into the normal build. Wolverine uses conventions to discover handlers and determine behavior. Its generated execution code is compiled at runtime by default, with static generation available as an alternative.

| Feature           | Mocha                                                                                                  | Wolverine                             |
| ----------------- | ------------------------------------------------------------------------------------------------------ | ------------------------------------- |
| License           | MIT                                                                                                    | MIT                                   |
| Handler discovery | Interfaces, checked at build time                                                                      | Naming conventions or markers         |
| Handler code      | Generated at build time                                                                                | Compiled at runtime, or pre-generated |
| Native AOT        | Yes                                                                                                    | With pre-generated code               |
| OpenTelemetry     | Yes                                                                                                    | Yes                                   |
| Test harness      | <FeatureStatus status="planned" href="https://github.com/ChilliCream/graphql-platform/issues/11038" /> | Yes                                   |
| Target frameworks | .NET 8 to 11                                                                                           | .NET 9 and 10                         |

These differences influence how you write, change, and deploy an application. With Wolverine, you can write short handlers and rely on the framework to recognize them. Once you're familiar with its conventions, that can make implementing messaging behavior feel very natural. With Mocha, you explicitly declare the relationships between handlers and messages. The compiler verifies those contracts, and the source generator handles registration during the build.

When a request changes, its response contract remains part of its type definition. When a saga changes, its states and transitions remain visible in the workflow definition. When handlers change, generated registration code is updated through the normal build process. The result is a development model where more of the messaging architecture is represented directly in C#.

**Wolverine reduces the amount you need to declare. Mocha increases the amount the compiler can verify.**

That distinction is more useful than simply comparing lines of code.

# Which One Should You Choose?

Mocha and Wolverine solve many of the same problems. Both let you build message-driven .NET applications with handlers, publish/subscribe messaging, request/reply, transactional outboxes, and sagas. Both use code generation to handle infrastructure that you would otherwise have to write yourself. And both make reasonable, deliberate choices about how developers should interact with a messaging framework. The deciding factor is what you want your framework to optimize for.

**Wolverine is a good fit if you prefer conventions that keep application code concise.**

Its handlers can be ordinary C# classes with methods that follow recognizable naming patterns. You don't need to implement an interface for every handler or define an explicit state machine for every saga. The framework interprets those conventions and connects the pieces. If your team is comfortable with that programming model, it can be an appealing way to write messaging code with relatively little ceremony.

Wolverine also supports more transports and databases today. If your application depends on Kafka, Amazon SQS/SNS, SQL Server, or MySQL, Mocha doesn't support them yet, and that's a practical limitation to consider. The feature tables above show where Mocha stands and which integrations are still planned.

The tradeoff is that some relationships in your application are established through framework conventions rather than compiler-checked contracts. Understanding those relationships requires knowing how Wolverine discovers methods, interprets return values, and connects generated code.

**Mocha is a compelling choice if you want messaging contracts to be explicit and checked by the compiler.**

Handlers declare which messages they process through interfaces. Requests define their expected response types. Sagas describe their states and transitions directly. And source generation happens during the normal build, so handler registration stays in sync with the code without a separate generation workflow.

You write a little more code to express those relationships. In return, more of your application's structure becomes visible to anyone reading it, and more mistakes can be caught before the application runs.

That tradeoff becomes particularly interesting in a world where coding agents increasingly contribute to application development. Writing an additional interface declaration or a few lines of state-machine configuration is a small task for an agent. Understanding the consequences of changing a handler, following a request through several services, or verifying a workflow is more demanding. Explicit contracts help with that work.

They give developers and coding agents a shared, compiler-checked description of how the pieces fit together. When those relationships change, the compiler can help identify code that needs attention.

Of course, explicit contracts don't replace tests, and compilation can't verify everything about a distributed system. Network failures, message ordering, and runtime behavior still need careful design. But when the compiler can verify something, we think it's worth giving it the information to do so.

That's ultimately why we designed Mocha the way we did. Not because conventions are inherently wrong, or because every additional line of code is valuable. Wolverine demonstrates why convention-based frameworks are attractive: they let you express useful behavior with remarkably little code. We simply prefer a different balance. For us, the small cost of declaring a handler interface, defining a response contract, or describing saga transitions explicitly is outweighed by the benefits of making those relationships visible and verifiable, especially in applications that will be maintained, refactored, and extended over time. So the choice comes down to one question:

**Would you rather write less code and rely on framework conventions, or write a few more explicit declarations and let the compiler check them for you?**

If you prefer explicit contracts, build-time generation, and messaging relationships that are visible directly in your C# code, that's exactly what Mocha is designed to provide.

**Our preference is to make the important relationships explicit, let the compiler verify what it can, and keep the code understandable for whoever works on it next, whether that's a developer or a coding agent.**
