---
title: Fusion
description: A showcase of every markdown feature.
---

## Heading Level 2

### Heading Level 3

#### Heading Level 4

##### Heading Level 5

###### Heading Level 6

## Paragraphs and Inline Formatting

This is a regular paragraph with **bold text**, _italic text_, **_bold and italic_**, ~~strikethrough~~, and `inline code`. You can also combine **_bold italic_** using underscores. Here is a hard line break:
end of line.

A second paragraph follows after a blank line. Inline HTML is also supported: <kbd>Ctrl</kbd> + <kbd>C</kbd>.

## Tabs

<Tabs>

<Tab label="npm">

npm stuff in here

```bash
npm do stuff
```

</Tab>

<Tab label="yarn">

```bash
yarn do stuff
```

yarn stuff in here

</Tab>

</Tabs>

## Links and References

- [Inline link](https://chillicream.com)
- [Link with title](https://chillicream.com "ChilliCream homepage")
- Plain URL: [https://chillicream.com](https://chillicream.com)
- Reference link: [Hot Chocolate][hc]
- [Jump to Tables](#tables)
- Footnote reference[^1]

[hc]: https://chillicream.com/docs/hotchocolate

[^1]: This is the footnote body.

## Lists

### Unordered

- Apples
- Oranges
  - Mandarin
  - Blood orange
- Pears

### Ordered

1. First
2. Second
   1. Nested second
   2. Another nested
3. Third

### Task List

- [x] Write the spec
- [x] Implement the parser
- [ ] Ship to production

### Definition List

Term
: Definition of the term.

GraphQL
: A query language for your API.

## Blockquotes

> Single-line blockquote.

> Multi-line blockquote with **formatting** and a [link](https://chillicream.com).
>
> > Nested blockquote.

## Code

Inline: `const answer = 42;`

### Plain code block

```ts
import { createServer } from "node:http";

const server = createServer((req, res) => {
  res.writeHead(200, { "Content-Type": "text/plain" });
  res.end("Hello, world!\n");
});

server.listen(3000);
```

### With filename

```graphql filename="schema.graphql"
type LineItem {
  id: Int!
  quantity: Int!
  productId: Int!
}

type Order {
  id: Int!
  name: String!
  items: [LineItem!]!
}

type Query {
  orders: [Order!]! @cost(weight: "10")
}
```

### Single line highlight `{5}`

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

### Multiple ranges `{1,3-5}`

```bash {1,3-5}
yarn install
yarn lint
yarn dev
yarn build
yarn start
yarn test
```

### Token highlighting + CodeStep

```csharp filename="Program.cs" [[1, 3, "Query"], [2, 5, "Hello"], [3, 11, "AddGraphQLServer"]]
using HotChocolate;

public class Query
{
    public string Hello() => "Hello, world!";
}

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>();

var app = builder.Build();
app.MapGraphQL();
app.Run();
```

A minimal Hot Chocolate setup wires up three things:

1. A <CodeStep step={1}>Query class</CodeStep> that defines the root operations of the schema.
2. Each public method like <CodeStep step={2}>Hello</CodeStep> becomes a GraphQL field on the root type.
3. The schema is registered through <CodeStep step={3}>AddGraphQLServer</CodeStep> in the DI configuration.

### All supported languages with badges

```csharp filename="Program.cs"
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddGraphQLServer();
var app = builder.Build();
app.MapGraphQL();
app.Run();
```

```bash
yarn install
yarn dev
```

```graphql
query GetUser($id: ID!) {
  user(id: $id) {
    id
    name
  }
}
```

```sdl
type Query {
  hello: String!
}
```

```http
GET /graphql HTTP/1.1
Host: localhost:5000
Content-Type: application/json
```

```json
{
  "name": "Tobias",
  "fans": ["graphql", "shiki"]
}
```

```sql
SELECT id, name FROM users WHERE active = TRUE ORDER BY id;
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
  unchanged
```

### Indented code block

    plain text
    no syntax highlighting

## Tables

| Feature   | Status |              Notes |
| --------- | :----: | -----------------: |
| Headings  |   ✅   |     All six levels |
| Tables    |   ✅   |     With alignment |
| Footnotes |   ✅   | See bottom of page |

## Horizontal Rule

---

## Images

![Alt text](https://chillicream.com/img/projects/greendonut-banner.svg "Title text")

## Raw HTML

<details>
  <summary>Click to expand</summary>

Hidden content with **markdown** still rendered inside.

</details>

<div align="center">

Centered block via raw HTML.

</div>

## Escapes

\*not italic\*, \`not code\`, \# not a heading.

## Admonitions (GitHub-style alerts)

> [!NOTE]
> Useful information that users should know.

> [!TIP]
> Helpful advice for doing things better.

> [!IMPORTANT]
> Key information users need to know.

> [!WARNING]
> Urgent info that needs immediate attention.

> [!CAUTION]
> Risk of negative outcomes.

## Emoji

:rocket: :tada: :sparkles:

## Keyboard

Press <kbd>Cmd</kbd> + <kbd>K</kbd> to open the command palette.

## Final Paragraph

That covers the markdown surface: headings, inline marks, links, lists, blockquotes, code, tables, images, raw HTML, admonitions, emoji, footnotes, and definitions.
