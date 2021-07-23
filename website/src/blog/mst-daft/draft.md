---
path: "/blog/2018/05/03/react-rasta-1.0.0"
date: "2018-05-03"
title: "Hot Chocolate Execution Engine"
tags: []
author: "Michael Staib"
authorUrl: https://github.com/rstaib
authorImageUrl: https://avatars0.githubusercontent.com/u/4325318?s=100&v=4
---

It is quite a while since I wrote my last piece. Usually I get into the mood of writing a blog post when our work is coming together an I can see the finish line of the next version. Our newest iteration of Hot Chocolate really packs a lot of new concepts and features and I will lay them out over the next weeks with a couple of blog posts.

Today lets talk about the new execution engine for V12. It is the 4th major iteration on the execution engine and it took us quite a while to get here. Maybe let me sum up a bit the evolution of the Hot Chocolate execution engine.

# Execution Engine 1

The initial execution engine really followed the implementation of graphql-js which is the reference implementation of the spec. Essentially when I started working on Hot Chocolate I really started understanding JavaScript and flow. My initial idea was to implement a basic GraphQL server over a two weeks period by porting a lot of graphql-js over to .NET. It is not a secret I think that my two weeks estimate was way off :)

Nevertheless, the first execution of Hot Chocolate was based on the ground work that so many OSS developers put into graphql-js. So how did this execution engine work? Essentially it took in a parsed request document and traversed the AST of it. Essentially you take a node execute the resolver for that node and go on traversing the tree passing down the result of a previous resolver.

The basic execution flow in GraphQL really is Parse -> Validate -> Execute. This actually is how many GraphQL servers still work. The spec however allows implementors to deviate from the execution algorithms layed out in the spec as long as the result is the same. Already at this stage I got the idea of optimizing the execution by creating a query plan that might actually rewrite what is executed and when.

# Execution Engine 2

With Hot Chocolate 6 I think we introduced pipeline concepts where each field does have instead of a resolver a resolver pipeline and the execution itself became a request pipeline. This was heavily inspired by the ASP.NET core request pipeline where you can write Middleware and put them together to extend and change the execution behavior.

Whenever you use features like paging or filtering in Hot Chocolate, this is actually based on a field pipeline. The attribute `UsePaging` for instance hooks in a middleware to the execution of a specific field. By this it can rewrite the result, fetch data, validate arguments and so on.

DIAGRAM

On the request level we also introduced this concept and the request processing became modular, the default request pipeline essentially was composed of some middleware.

DIAGRAM

With this feature we were able to create features like schema stitching which was introduce with Hot Chocolate 8 and is based on these middleware concept. Essentially schema stitching hooks some resolvers in that rewrite a subtree of a GraphQL document and branches that of to send it to another GraphQL server. The response is then rewritten to fit the current schema and the rest of the execution is then done by the standard Hot Chocolate GraphQL core. So really pretty simple. The good thing here is that by doing this you can merge remote and local schemas since schema stitching really is just standard Hot Chocolate.

DIAGRAM/PICTURE

There are many issues with this approach since we could optimize data fetching better if the execution engine had a way to understand and optimize. Essentially the execution engine would need to understand the request itself and all the downstream services. With this I mean that the execution engine would need to understand that sometime it is more efficient to overfetch on one of the downstream services for a specific resolver since it then could reuse that overfetched data in another place.

# Execution Engine 3

While we were constantly updating the execution engine it took until Hot Chocolate 11 to fundamentally change. With version 11 we introduced three new concepts that also really delivered on performance.

DIAGRAM PIPELINE

## Compiled Operations

Instead of execution on the AST we compile the part that are needed for the execution of the currently selected operation into a new tree that represents this operation. The compiled tree has no more any notion of fragments. The `@skip` and `@include` directives are compiled into visibility hints. There is one exception to the fragments, we compile deferred fragments into sub operation trees. You can feed them into the same executor.

The operation compiler allows components to inject selection optimizer which allows components to rewrite parts of the execution tree by retaining the result structure. Selection optimizers are for instance used by the projection middleware to overfetch on certain fields to get internal ids that are needed to further fetch data from the database. Selection optimizer can be globally injected or annotated to fields. We also use them to optimize defer logic. For instance if you fetch only scalars in a deferred fragment that are loaded anyway in the initial call we will just fold the execution of these fields into the main execution.

The operation compiler is still no query plan but it already allowed us to create unique new features by allowing components to introduce internal selections or rewrite the execution structure.

## Execution Tasks

The V11 execution engine also walked away from executing fields. The execution engine executes `IExecutionTask` and components can register execution tasks that the execution engine will just process. This allowed us to abstract the tasks that the execution engine has to do into abstract units. Fields, batching requests and other data fetching is just a task. Task can produce other tasks. However, They did not have a specific order, the execution engine would just start executing tasks until there are no more tasks to execute.

## Batching

Lastly, we introduced a new service that components can inject that allows to register batches on the execution engine. Whenever the execution engine runs out of things to process it would dispatch those batches. Batches are as you might have guessed just execution tasks that are enqueued. You could think of these as tasks that are held back for a while. By introducing `IBatchScheduler` we essentially where able to decouple components that we needed to execute batches for schema stitching requests, DataLoader, and other batch data fetching.

# Execution Engine 4 - Version 12

## Query Plan
