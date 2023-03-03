---
title: Overview
description: In this section, we will learn about the Hot Chocolate GraphQL server execution engine.
---

# Field middleware

Field middleware allows us to create reusable logic that is run before or after a resolver. It also allows us to access or even modify the result produced by a resolver.

[Learn more about field middleware](/docs/hotchocolate/v13/execution-engine/field-middleware)

# Request middleware

The GraphQL execution is abstracted into a request pipeline composed of many request middleware. Each request middleware represents one part of executing a GraphQL request, like the parsing of the GraphQL request document or the semantical validation of the GraphQL request document.

[Learn more about request middleware](/docs/hotchocolate/v13/execution-engine/request-middleware)

# Global state

TODO

[Learn more about global state](/docs/hotchocolate/v13/execution-engine/global-state)

# Scoped state

TODO

[Learn more about scoped state](/docs/hotchocolate/v13/execution-engine/scoped-state)

# Local state

Local state allows you to share data between field middleware and the actual resolver.

[Learn more about local state](/docs/hotchocolate/v13/execution-engine/local-state)

# Resolver compiler

The resolver compiler will compile for each resolver an optimized resolver pipeline. The resolver compiler can be customized by providing parameter expression builder.

[Learn more about the resolver compiler](/docs/hotchocolate/v13/execution-engine/resolver-compiler)
