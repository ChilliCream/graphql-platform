---
title: Overview
---

In this section we will learn about the Hot Chocolate execution engine.

<iframe width="560" height="315"
src="https://www.youtube.com/embed/Ut33sSTYmgw"frameborder="0"
allowfullscreen></iframe>

# Request Middleware

The GraphQL execution is abstraction into a request pipeline that is composed of many request middleware. Each request middleware represents one part of executing a GraphQL request like the parsing of the GraphQL request document or the semantical validation of the GraphQL document.

# Field middleware

Field middleware allows us to create reusable logic that is run before or after a resolver. It also allows us to access or even modify the result produced by a resolver.

[Learn more about field middleware](/docs/hotchocolate/execution-engine/field-middleware)
