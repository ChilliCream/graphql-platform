---
title: Overview
---

In this section we will learn about the Hot Chocolate execution engine.

# Request Middleware

The GraphQL execution is abstracted into a request pipeline composed of many request middleware. Each request middleware represents one part of executing a GraphQL request, like the parsing of the GraphQL request document or the semantical validation of the GraphQL request document.

<iframe width="560" height="315"
src="https://www.youtube.com/embed/Ut33sSTYmgw"frameborder="0"
allowfullscreen></iframe>

# Field middleware

Field middleware allows us to create reusable logic that is run before or after a resolver. It also allows us to access or even modify the result produced by a resolver.

[Learn more about field middleware](/docs/hotchocolate/v13/execution-engine/field-middleware)

# Resolver Compiler

The resolver compiler will compile for each resolver an optimized resolver pipeline. The resolver compiler can be customized by providing parameter expression builder.

<iframe width="560" height="315"
src="https://www.youtube.com/embed/C2YSeVK6Dck"frameborder="0"
allowfullscreen></iframe>
