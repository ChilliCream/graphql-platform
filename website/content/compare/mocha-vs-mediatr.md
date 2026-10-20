---
title: "Mocha vs MediatR"
metaTitle: "Mocha vs MediatR: An MIT-Licensed MediatR Alternative"
description: "Compare the Mocha mediator with MediatR for commands, queries, and notifications in .NET: licensing, source-generated dispatch, pipelines, and migration."
product: "mocha"
competitor: "MediatR"
lastChecked: "2026-10-20"
versions:
  product: "16.6"
  competitor: "14.2"
migrationGuide: "/docs/mocha/migration/coming-from-mediatr"
highlights:
  - title: "MIT licensed, no license key"
    text: "Mocha is free for any company. Since version 13, MediatR is licensed under the copyleft RPL-1.5 or a commercial license, which is free only for smaller companies."
  - title: "Handlers registered at build time"
    text: "A source generator writes the registration code for every handler when you compile. MediatR scans assemblies at startup and builds handler wrappers through reflection."
  - title: "Commands and queries are separate"
    text: "ICommand and IQuery replace IRequest, so reads and writes are different types with different dispatch methods, and middleware can target either one."
  - title: "A message bus when you need one"
    text: "The same framework sends events and commands between services, with sagas, an outbox, and retries. MediatR handles in-process messages only."
features:
  - title: "Licensing"
    rows:
      - label: "License"
        product: "MIT"
        competitor: "RPL-1.5 or commercial, since 13.0"
      - label: "Commercial license"
        product: "Not needed"
        competitor: "$499 to $3,999 per year"
      - label: "Free commercial use"
        product: "Any company"
        competitor: "Under $5M annual revenue"
      - label: "License key in production"
        product: "Not needed"
        competitor: "Logs a warning without one"
  - title: "Messages"
    rows:
      - label: "Commands without a response"
        product: true
        competitor: true
      - label: "Commands with a response"
        product: true
        competitor: true
      - label: "Queries as their own type"
        product: true
        competitor: false
      - label: "Notifications"
        product: true
        competitor: true
      - label: "Stream requests"
        product: false
        competitor: true
  - title: "Pipeline"
    rows:
      - label: "Cross-cutting behavior"
        product: "Middleware, filtered per message type"
        competitor: "Typed generic behaviors"
      - label: "Pre- and post-processors"
        product: "As middleware"
        competitor: true
      - label: "Exception handlers"
        product: "As middleware"
        competitor: true
      - label: "Concurrent notification handlers"
        product: true
        competitor: true
      - label: "Custom notification publishers"
        product: false
        competitor: true
      - label: "Polymorphic notification handlers"
        product: false
        competitor: true
  - title: "Runtime"
    rows:
      - label: "Handler registration"
        product: "Source generator at build time"
        competitor: "Assembly scanning at startup"
      - label: "Default handler lifetime"
        product: "Scoped"
        competitor: "Transient"
      - label: "Target frameworks"
        product: ".NET 8 to 11"
        competitor: ".NET 8 to 10, .NET Standard 2.0, .NET Framework 4.6.2"
      - label: "Messaging between services"
        product: true
        competitor: false
sources:
  - title: "MediatR license (LICENSE.md)"
    url: "https://github.com/LuckyPennySoftware/MediatR/blob/main/LICENSE.md"
  - title: "Lucky Penny Software license agreement"
    url: "https://luckypennysoftware.com/license"
  - title: "Lucky Penny Software licensing FAQ"
    url: "https://luckypennysoftware.com/faq"
  - title: "MediatR editions and pricing"
    url: "https://mediatr.io/"
  - title: "AutoMapper and MediatR going commercial, Jimmy Bogard, April 2, 2025"
    url: "https://www.jimmybogard.com/automapper-and-mediatr-going-commercial/"
  - title: "MediatR 13.0.0 release notes"
    url: "https://github.com/LuckyPennySoftware/MediatR/releases/tag/v13.0.0"
  - title: "MediatR 14.2.0 on NuGet"
    url: "https://www.nuget.org/packages/MediatR/14.2.0"
  - title: "MediatR license key configuration"
    url: "https://github.com/LuckyPennySoftware/MediatR/wiki/License-Key-Configuration"
  - title: "MediatR wiki: requests, notifications, and behaviors"
    url: "https://github.com/LuckyPennySoftware/MediatR/wiki"
  - title: "MediatR source: Mediator.cs at 14.2.0"
    url: "https://github.com/LuckyPennySoftware/MediatR/blob/v14.2.0/src/MediatR/Mediator.cs"
---

# Licensing

MediatR changed its license with version 13.0 in July 2025. Versions up to 12.5 stay under the Apache 2.0 license, but Lucky Penny Software, the company behind MediatR, no longer provides support or security updates for them. Version 13 and later are dual licensed. You either accept the Reciprocal Public License 1.5, a copyleft license, or buy a commercial license.

The commercial license is free as the Community edition if your company has less than $5 million in annual revenue, has raised no more than $10 million in outside capital, and is not a government body, among other conditions. Otherwise it costs $499 per year for up to 10 developers, $1,499 for up to 50, and $3,999 for an unlimited number. Production deployments are expected to set a license key. Without one, MediatR logs a warning and keeps working.

Mocha is MIT licensed, like Hot Chocolate. There is no paid edition and no license key.

# How dispatch works

MediatR finds handlers by scanning your assemblies when the application starts. The first time a request type is sent, it creates a handler wrapper through reflection and caches it, then resolves the handler and its behaviors from the service provider on every call.

Mocha does this work at build time. The `Mocha.Analyzers` source generator finds the handlers in your project and writes a registration method, such as `AddOrdering()`, that registers each handler with a pipeline built from generic code. Dispatch needs no reflection, and the mediator packages are marked compatible with Native AOT. The generator also reports a message type without a handler, or with more than one, as a build diagnostic.

# Commands, queries, and notifications

MediatR models every request as `IRequest` or `IRequest<TResponse>`. Mocha separates the two intents. `ICommand` and `ICommand<TResponse>` change state and are dispatched with `SendAsync`. `IQuery<TResponse>` only reads and is dispatched with `QueryAsync`. Notifications work the same in both: `INotification` with any number of handlers, run one after another by default or concurrently.

# Pipelines

A MediatR behavior is a generic class, `IPipelineBehavior<TRequest, TResponse>`, resolved from the container on every request. Pre-processors, post-processors, and exception handlers are separate interfaces on top of that.

Mocha has one concept for all of these: middleware. A middleware factory runs once per message type when the pipeline is built. It can leave message types out, for example every query, and returns the delegate that runs on each dispatch. Code before `next` replaces a pre-processor, code after it replaces a post-processor, and a `try`/`catch` around it replaces an exception handler.

# When MediatR is the better fit

- **You use stream requests.** Mocha has no equivalent of `IStreamRequest`.
- **You rely on polymorphic notification handlers or a custom notification publisher.** A Mocha notification handler runs only for the exact type it declares, and the publish strategy is one of two built-in modes.
- **You target .NET Framework or .NET Standard.** Mocha requires .NET 8 or later.
- **Your pipeline is built from typed generic behaviors,** including third-party ones written for MediatR, and rewriting them as middleware is not worth the effort.
- **You qualify for the Community edition** or can accept the RPL-1.5, and a migration costs you more than it saves.

# Moving from MediatR

Most of a migration is renaming. Requests become commands or queries, `Handle` becomes `HandleAsync` and returns a `ValueTask`, and `AddMediatR` becomes `AddMediator` with the generated registration method.

| MediatR                                  | Mocha                                                                        |
| ---------------------------------------- | ---------------------------------------------------------------------------- |
| `IRequest<TResponse>`                    | `ICommand<TResponse>` or `IQuery<TResponse>`                                 |
| `IRequestHandler<TRequest, TResponse>`   | `ICommandHandler<TCommand, TResponse>` or `IQueryHandler<TQuery, TResponse>` |
| `INotificationHandler<T>`                | `INotificationHandler<T>`                                                    |
| `ISender.Send`                           | `ISender.SendAsync` and `ISender.QueryAsync`                                 |
| `IPublisher.Publish`                     | `IPublisher.PublishAsync`                                                    |
| `IPipelineBehavior<TRequest, TResponse>` | Mediator middleware                                                          |

The [MediatR migration guide](../docs/mocha/migration/coming-from-mediatr.md) walks through each step with code, including how to run both libraries side by side while you move handlers over.
