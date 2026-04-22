---
path: "/blog/2026/04/22/semantic-introspection"
date: "2026-04-22"
title: "Semantic Introspection"
description: "The agentic age of software brings new challenges for our APIs. Semantic Introspection makes GraphQL discoverable, scalable, and precise for LLMs."
tags: ["hotchocolate", "graphql", "ai", "llm", "semantic-introspection"]
featuredImage: "header.png"
author: Pascal Senn
authorUrl: https://github.com/pascalsenn
authorImageUrl: https://avatars.githubusercontent.com/u/14233220?v=4
---

The agentic age of software has just begun, and it brings a whole new set of challenges for our applications.
Until recently, the consumers of our APIs, web apps, and mobile apps were human users. Going forward, our APIs will increasingly be consumed by LLMs.

Where we used to optimize for request performance, time to first byte, and 3G performance, we now have to think about context window size, LLM cost, turn reduction, and hallucinations.

It is interesting to see that the classic Lighthouse metrics we spent so much effort perfecting are largely irrelevant for LLMs. The sweat, blood, and tears we poured into pushing those four numbers close to 100% do not add much value for large language models. It does not matter if your data fetch takes less than 500ms when the LLM needs 15 seconds to process the data and generate a response per turn. What matters now is reducing the number of turns. Your API response should include every piece of relevant information, but it should not include more than that, because every extra byte pollutes the context of your LLM.

For an agent to interact with an API, the interface has to be three things: **discoverable**, **scalable**, and **precise**.

**Discoverable**

Your application has functionality that can be accessed through some sort of interface. If that application wants to interact with an LLM it needs an API. But having an API is not enough. The LLM has to know what functionality the API provides and have a way to discover its capabilities. This could be an OpenAPI document for REST, the `list_tools` tool from MCP, or GraphQL introspection.

**Scalable**

Applications become increasingly more capable. With the introduction of agents, the age of software really has begun. When the cost of adding a feature drops, we naturally add more features. The consequence is that more and more capabilities end up exposed through our APIs. The interaction model between the API and the agents therefore needs to scale with the amount of available capabilities. The agent should work with 10 available tools, but it should also work the same way when there are 3000.

**Precise**

Every byte returned by an API has to be processed by an LLM to extract the information it needs. Data returned by an API stays in the context of the model and is sent to the LLM on every subsequent roundtrip. The more context we send, the more input tokens we pay for. At the same time, we want to avoid additional roundtrips whenever possible, because every roundtrip means sending the context again and waiting another 20 seconds for the LLM to respond. We want a precise API that returns all the data we need, and nothing we do not.

There are currently different interaction models for agents and APIs.

Looking at the API ecosystem today, the most common technology for API documentation is OpenAPI. Through an OpenAPI document an agent can _discover_ the available endpoints, the required parameters, and the expected responses. However, the agent has to either load the whole OpenAPI document into the LLM context, or store it on disk and search through it with `grep` or something similar, which leads to a lot of roundtrips. On top of that, the responses are fixed. There is no way to dynamically adjust what the server returns. All of this leads to context pollution and, combined with the extra roundtrips, higher cost.

The AI ecosystem has been focused on MCP over the past months. MCP is already natively integrated with all major LLM providers, and through `list_tools` agents can _discover_ the available tools. Just like OpenAPI, MCP is not _precise_. The response is fixed, and the agent has to pass it to the LLM for verification. MCP also does not _scale_ well. To make use of an MCP server, the whole tool directory has to be sent to the LLM so it has a directory of the available tools. The orchestrator on your machine or browser does not know if the LLM will reply with a tool call or a normal response, so it has to send the whole tool directory on every roundtrip. This adds a lot of input tokens and cost, and it only works while the tool directory stays small. If it gets too big, the LLM cannot process it because it exceeds the context window. It is not just us that have noticed these problems. Even Anthropic, the creator of MCP, has [acknowledged the flaws](https://www.anthropic.com/engineering/code-execution-with-mcp).

As an alternative to MCP, Anthropic pushes skills or recommends just using CLIs. The issue with these approaches is that they have a higher barrier to entry. Configuring an MCP server is simple even for non technical users, but using a CLI tool is not, especially if they do not know what a terminal is.

So, what about GraphQL? One of the biggest marketing points of GraphQL has always been the "no overfetching" promise. By writing a query, we can specify exactly what data we want from the server, nothing more and nothing less. With Fusion, this data can even be spread across many different backend services, while the client still interfaces with what looks like a single API. (You can check out a sample repository [here](https://github.com/PascalSenn/apidays-singapore), where we combine several APIs from data.gov.sg.) This makes GraphQL a very _precise_ API. In that regard, it does not suffer from context pollution like OpenAPI or MCP.

Another core feature built into GraphQL from day one is its introspection capabilities. With GraphQL introspection, an agent can _discover_ the schema of the API and learn exactly which queries and mutations are available, what arguments they take, and what data they return.

Yet, like all other technologies, GraphQL has the _scale_ problem. While a GraphQL schema is more compact than an OpenAPI schema, it can still become too big for an LLM to process, and sending it on every turn adds cost.

This is where [**Semantic Introspection**](https://github.com/graphql/ai-wg/blob/main/rfcs/semantic-introspection.md) comes in. Semantic Introspection is a proposed extension to GraphQL introspection.

Semantic Introspection adds a new field to the GraphQL server, `__search(query: "query text")`. With this field, an agent can ask the server a question, and the server returns the schema members that best match semantically. If the user asks the LLM "What's the weather like in Bedok today and are there any taxis available?", the agent can forward the question to the server via `__search`.

```graphql
{
  __search(query: "What's the weather like in Bedok today and are there any taxis available", first: 10) {
    coordinate
    score
    pathsToRoot
    definition {
      __typename
      ... on __Field {
        # left out for brevity
      }
      ... on __Type {
        # left out for brevity
      }
    }
  }
}
```

The GraphQL server then returns the best matching schema members ranked by score.

```json
{
  "data": {
    "__search": [
      {
        "coordinate": "Area.availableTaxis",
        "score": 1,
        "pathsToRoot": [["Query.areaByName", "Area.availableTaxis"]],
        "definition": {
          "__typename": "__Field",
          "fieldName": "availableTaxis",
          "description": "Returns the number of available taxis in the area",
          "type": {
            "name": null,
            "kind": "NON_NULL",
            "ofType": {
              "name": "Int",
              "kind": "SCALAR"
            }
          },
          "args": []
        }
      },
      {
        "coordinate": "WeatherStation",
        "score": 0.5979468822479248,
        "pathsToRoot": [["Query.areaByName", "Area.nearestStation"]],
        "definition": {
          "__typename": "__Type",
          "name": "WeatherStation",
          "kind": "OBJECT",
          "description": "A weather station that provides weather information for an area"
        }
      }
      // left out for brevity
    ]
  }
}
```

The LLM now knows which parts of the schema are relevant for the user query. Thanks to the precomputed paths to root, the agent also knows how to reach the relevant parts of the schema from the query root. To know how to build a query, the LLM needs a bit more detail about the path. It can use `__definitions(coordinates: ["Query.areaByName", "Area.nearestStation"])` to fetch the details for those coordinates.

Put together, this makes GraphQL's _discovery_ capabilities _scalable_ too. Discovery of any capability becomes a simple two-step process: first, search for the relevant capabilities with `__search`, then fetch the details with `__definitions`. The process stays the same whether your schema has 10 types or 1000. By providing descriptions for types and fields, you can also make the search more effective and improve the score of relevant schema members, simply by improving the documentation of your schema.

If we run a small experiment comparing the cost of discovery across OpenAPI, MCP, and GraphQL with Semantic Introspection, GraphQL with Semantic Introspection comes out significantly more cost effective than the other two approaches.

| Discovery Approach                  | Tokens sent to LLM | Cost (USD) |
| ----------------------------------- | ------------------ | ---------- |
| OpenAPI                             | 665,564            | $0.3950    |
| GraphQL Schema                      | 133,441            | $0.1072    |
| GraphQL with Semantic Introspection | 59,067             | $0.0895    |

The latest Hot Chocolate preview already supports Semantic Introspection. You can just turn it on with `.ModifyOptions(x => x.EnableSemanticIntrospection = true)`. By default it indexes the schema with BM25, which comes at no additional cost. We will soon provide an option to hook the semantic search up to Nitro and back it with embeddings, which will provide even better search results.

Check out the demo repository with all the code here: [Semantic Introspection Demo](https://github.com/PascalSenn/apidays-singapore) and let us know what you think about Semantic Introspection!
