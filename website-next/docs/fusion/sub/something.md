---
title: Code block showcase
---

# Code blocks

## Plain TypeScript

```ts
const greet = (name: string) => `Hello, ${name}!`;
console.log(greet("world"));
```

## With filename

```graphql filename="schema.graphql"
type LineItem {
  id: Int!
  quantity: Int!
  productId: Int!
}

type Query {
  orders: [Order!]! @cost(weight: "10")
}
```

## Line highlighting with `{5}`

```js {5}
export default function MyApp() {
  return (
    <div>
      <h1>Welcome to my app</h1>
      <MyButton />
    </div>
  );
}
```

## Multiple ranges `{1,3-5}`

```bash {1,3-5}
yarn install
yarn dev
yarn build
yarn start
yarn lint
```

## Token highlighting + CodeStep prose

```js [[1, 7, "count"], [2, 7, "dispatchAction"], [3, 7, "isPending"]]
import { useActionState } from 'react';

async function addToCartAction(prevCount) {
  // ...
}
function Counter() {
  const [count, dispatchAction, isPending] = useActionState(addToCartAction, 0);

  // ...
}
```

`useActionState` returns an array with exactly three items:

1. The <CodeStep step={1}>current state</CodeStep>, initially set to the initial state you provided.
2. The <CodeStep step={2}>action dispatcher</CodeStep> that lets you trigger the action.
3. A <CodeStep step={3}>pending state</CodeStep> that tells you whether the action is in progress.

## C# / SDL / SQL / JSON / HTTP / XML / Diff

```csharp filename="Program.cs"
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddGraphQLServer();
var app = builder.Build();
app.MapGraphQL();
app.Run();
```

```sdl
type Query {
  hello: String!
}
```

```sql
SELECT id, name FROM users WHERE active = TRUE;
```

```json
{ "name": "Tobias", "fans": ["graphql", "shiki"] }
```

```http
GET /graphql HTTP/1.1
Host: localhost:5000
```

```xml
<note>
  <to>Reader</to>
  <body>Hello!</body>
</note>
```

```diff
- old line
+ new line
```
