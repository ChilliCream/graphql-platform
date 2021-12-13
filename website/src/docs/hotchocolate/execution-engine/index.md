---
title: Overview
---

In this section we will learn about the features of the Hot Chocolate execution engine.

# Field middleware

Field middleware allows us to create reusable logic that is run before or after a resolver. It also allows us to access or even modify the result produced by a resolver.

[Learn more about field middleware](/docs/hotchocolate/execution-engine/field-middleware)

# Request pipeline

The Hot Chocolate execution flow is fully modular and defined by the request pipeline. The request pipeline consists of multiple request middleware. A request middleware provides us the main component to customize the request execution flow.

[Learn more about the request pipeline](/docs/hotchocolate/execution-engine/request-pipeline)
