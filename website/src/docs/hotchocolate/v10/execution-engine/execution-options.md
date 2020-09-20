---
title: Execution Options
---

Execution options are provided when a schema is made executable. The options range from allowing a maximum execution timeout to providing a maximum execution complexity.

We have built in some options that limit the execution engine in order to protect the overall performance of your GraphQL Server.

# Members

| Member                   | Type     | Default                    | Description                                                                   |
| ------------------------ | -------- | -------------------------- | ----------------------------------------------------------------------------- |
| EnableTracing            | bool     | `false`                    | Enables tracing for performance measurement of query requests. _\*_           |
| ExecutionTimeout         | TimeSpan | `TimeSpan.FromSeconds(30)` | The maximum allowed execution time of a query.                                |
| IncludeExceptionDetails  | bool     | `Debugger.IsAttached`      | Includes exception details into the GraphQL errors. _\*\*_                    |
| MaxExecutionDepth        | int?     | `null`                     | The maximum allowed query depth of a query.                                   |
| QueryCacheSize           | int      | `100`                      | The amount of queries that can be cached for faster execution.                |
| MaxOperationComplexity   | int?     | null                       | The allowed complexity of queries.                                            |
| UseComplexityMultipliers | bool?    | null                       | Specifies if multiplier arguments are used to calculate the query complexity. |
| ForceSerialExecution     | bool?    | null                       | Used for EntityFramework to have request be done in one thread.               |

_\* Performance tracing is based on Apollo Tracing. The specification can be found [here](https://github.com/apollographql/apollo-tracing)._

_\*\* The exception details that are included into GraphQL errors can also be modified by implementing an `IErrorFilter`. See more about that [here](/docs/hotchocolate/v10/execution-engine/error-filter)._
