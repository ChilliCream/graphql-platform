---
title: "Banana Cake Pop - Express"
---

You can easily integrate Banana Cake Pop GraphQL IDE with your server app using the `@chillicream/bananacakepop-express-middleware` package.
You can either use a CDN hosted version of the app or a self-hosted version using the dedicated package.

# Installation

First, you need to install this package and the required peer dependencies in your project:

```bash
npm install @chillicream/bananacakepop-express-middleware --save-dev
# or
yarn add @chillicream/bananacakepop-express-middleware --dev
# or
pnpm add @chillicream/bananacakepop-express-middleware --save-dev
```

Note: `@chillicream/bananacakepop-graphql-ide` is optional and only needed if you prefer to self host the app.

# Usage

To use the middleware, simply import it and add it to your Express app.

```javascript
import express from "express";
import { graphqlHTTP } from "express-graphql";
import { GraphQLObjectType, GraphQLSchema, GraphQLString } from "graphql";

import bcpMiddleware from "@chillicream/bananacakepop-express-middleware";

const schema = new GraphQLSchema({
  query: new GraphQLObjectType({
    name: "Query",
    fields: {
      greeting: {
        type: GraphQLString,
        resolve(_parent, _args) {
          return "Hello, World!";
        },
      },
    },
  }),
});

const app = express();

app.use(
  "/graphql",

  // for `cdn` hosted version
  bcpMiddleware({ mode: "cdn" }),

  // for `self` hosted version
  // bcpMiddleware({ mode: "self" }),

  graphqlHTTP({
    schema,
    graphiql: false,
  })
);

app.listen(3000, () => {
  console.log(`GraphQL on http://localhost:3000/graphql`);
});
```

You can also use it in the `self` mode for a self-hosted version:

```javascript
bcpMiddleware({ mode: "self" }) // for `self` hosted version
```

# Extended configuration

## Pin a specific version
To pin a specific version instead of using "latest":

```javascript
bcpMiddleware({
  mode: "cdn",
  target: { version: "3.0.0" },
});
```

## Use your own infrastructure
To use your own infrastructure:

```javascript
bcpMiddleware({
  mode: "cdn",
  target: "https://mycompany.com/bcp",
});
```

## Custom options
To pass options supported by Banana Cake Pop GraphQL IDE:

```javascript
bcpMiddleware({
  mode: "cdn",
  options: {
    title: "BCP",
  },
});
```

| Property                | Description                                                   | Type                              |
| ----------------------- | ------------------------------------------------------------- | --------------------------------- |
| title                   | The title of the Banana Cake Pop IDE.                         | `string` (optional)               |
| graphQLDocument         | Specifies the GraphQL document (query/mutation/subscription). | `string` (optional)               |
| variables               | Specifies the variables used in the GraphQL document.         | `Record<string, any>` (optional)  |
| includeCookies          | If `true`, includes cookies in the request.                   | `boolean` (optional)              |
| httpHeaders             | Specifies HTTP headers for the request.                       | `HttpHeaderDictionary` (optional) |
| endpoint                | The GraphQL endpoint.                                         | `string` (optional)               |
| useGet                  | If `true`, uses GET method for sending the request.           | `boolean` (optional)              |
| useBrowserUrlAsEndpoint | If `true`, uses the browser's URL as the GraphQL endpoint.    | `boolean`                         |
| subscriptionProtocol    | Specifies the protocol used for GraphQL subscriptions.        | `SubscriptionProtocol` (optional) |

# Recipes
Below are examples of how to use Banana Cake Pop Express Middleware with different GraphQL server setups.

## graphql-http
```javascript
import express from "express";
import { createHandler } from "graphql-http";
//... rest of the imports

const app = express();
//... rest of the app setup

app.use(
  "/graphql",
  bcpMiddleware({ mode: "cdn" }), // or bcpMiddleware({ mode: "self" }),
  async (req, res) => {
    //... rest of the middleware
  }
);

app.listen(3000, () => {
  console.log(`GraphQL on http://localhost:3000/graphql`);
});
```

## graphql-yoga
```javascript
import express from "express";
import { createYoga, createSchema } from "graphql-yoga";
//... rest of the imports

const app = express();
//... rest of the app setup

app.use(
  "/graphql",
  bcpMiddleware({ mode: "cdn" }), // or bcpMiddleware({ mode: "self" }),
  graphQLServer
);

app.listen(3000, () => {
  console.log(`GraphQL on http://localhost:3000/graphql`);
});
```

## express-graphql
```javascript
import express from "express";
import { graphqlHTTP } from "express-graphql";
//... rest of the imports

const app = express();
//... rest of the app setup

app.use(
  "/

graphql",
  bcpMiddleware({ mode: "cdn" }), // or bcpMiddleware({ mode: "self" }),
  graphqlHTTP({
    schema,
    graphiql: false,
  })
);

app.listen(3000, () => {
  console.log(`GraphQL on http://localhost:3000/graphql`);
});
```

## Apollo Server
```javascript
import { ApolloServer } from "@apollo/server";
//... rest of the imports

const app = express();
//... rest of the app setup

app.use(
  "/graphql",
  bcpMiddleware({ mode: "cdn" }), // or bcpMiddleware({ mode: "self" }),
  cors(),
  bodyParser.json(),
  expressMiddleware(server, {
    context: async ({ req }) => ({ token: req.headers.token }),
  })
);

httpServer.listen({ port: 3000 }, () => {
  console.log(`GraphQL on http://localhost:3000/graphql`);
});
```
