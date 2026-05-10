import type { Meta, StoryObj } from "@storybook/nextjs-vite";
import type { ReactElement } from "react";
import { CodeBlock } from "./CodeBlock";
import { renderBlock } from "./codeBlockStoryUtils";

const meta = {
  title: "Design System/CodeBlock/Languages",
  component: CodeBlock,
} satisfies Meta<typeof CodeBlock>;

export default meta;
type Story = StoryObj<typeof meta>;

function languageStory(
  language: string,
  code: string,
  meta?: string,
  options: { name?: string } = {}
): Story {
  return {
    ...(options.name ? { name: options.name } : {}),
    loaders: [async () => ({ rendered: await renderBlock(language, code, meta) })],
    render: (_args, ctx) => ctx.loaded.rendered as ReactElement,
  };
}

const tsxSample = `import { useState } from "react";

export function Counter() {
  const [count, setCount] = useState(0);
  return (
    <button onClick={() => setCount(count + 1)}>
      Clicked {count} times
    </button>
  );
}`;

const jsxSample = `import { useState } from "react";

export function Counter() {
  const [count, setCount] = useState(0);
  return (
    <button onClick={() => setCount(count + 1)}>
      Clicked {count} times
    </button>
  );
}`;

const csharpSample = `public sealed class Query
{
    public Book GetBook() =>
        new Book("C# in Depth", new Author("Jon Skeet"));
}

public sealed record Book(string Title, Author Author);
public sealed record Author(string Name);`;

const bashSample = `dotnet add package HotChocolate.AspNetCore
dotnet build
dotnet run --urls=http://localhost:5000`;

const graphqlSample = `query GetBook($id: ID!) {
  book(id: $id) {
    title
    author {
      name
    }
  }
}`;

const sdlSample = `type Query {
  book(id: ID!): Book
}

type Book {
  title: String!
  author: Author!
}

type Author {
  name: String!
}`;

const httpSample = `POST /graphql HTTP/1.1
Host: api.chillicream.com
Content-Type: application/json
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9

{"query":"{ book { title } }"}`;

const jsonSample = `{
  "data": {
    "book": {
      "title": "C# in Depth",
      "author": { "name": "Jon Skeet" }
    }
  }
}`;

const sqlSample = `SELECT b.id, b.title, a.name AS author
FROM books AS b
JOIN authors AS a ON a.id = b.author_id
WHERE b.published_at >= '2020-01-01'
ORDER BY b.published_at DESC
LIMIT 10;`;

const htmlSample = `<!doctype html>
<html lang="en">
  <head>
    <meta charset="utf-8" />
    <title>Books</title>
    <link rel="stylesheet" href="/styles.css" />
  </head>
  <body>
    <main class="library">
      <h1>Library</h1>
      <article id="book-42" class="book">
        <h2>C# in Depth</h2>
        <p>By <a href="/authors/jon-skeet">Jon Skeet</a></p>
      </article>
    </main>
  </body>
</html>`;

const cssSample = `:root {
  --color-bg: #0d1117;
  --color-fg: #c9d1d9;
  --radius: 0.5rem;
}

.library {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(16rem, 1fr));
  gap: 1rem;
  padding: 2rem;
  background: var(--color-bg);
  color: var(--color-fg);
}

.book:hover {
  transform: translateY(-2px);
  border-radius: var(--radius);
}`;

const xmlSample = `<?xml version="1.0" encoding="UTF-8"?>
<library>
  <book id="42">
    <title>C# in Depth</title>
    <author>Jon Skeet</author>
  </book>
</library>`;

const diffSample = `   public sealed class Query
   {
-      public Book GetBook() => new Book("C# in Depth");
+      public Book GetBook() =>
+          new Book("C# in Depth", new Author("Jon Skeet"));
   }`;

const mermaidSample = `flowchart LR
  Client["GraphQL Client"] -->|HTTP| Gateway
  Gateway -->|sub-query| BooksSchema["Books Schema"]
  Gateway -->|sub-query| AuthorsSchema["Authors Schema"]
  BooksSchema --> DB[(Books DB)]
  AuthorsSchema --> Identity[(Identity DB)]`;

export const TypeScript: Story = languageStory(
  "tsx",
  tsxSample,
  'filename="Counter.tsx"',
  { name: "TypeScript" }
);

export const JavaScript: Story = languageStory(
  "jsx",
  jsxSample,
  'filename="Counter.jsx"',
  { name: "JavaScript" }
);

export const CSharp: Story = languageStory(
  "csharp",
  csharpSample,
  'filename="Query.cs"',
  { name: "C#" }
);

export const Bash: Story = languageStory("bash", bashSample);

export const GraphQL: Story = languageStory(
  "graphql",
  graphqlSample,
  'filename="GetBook.graphql"',
  { name: "GraphQL" }
);

export const SDL: Story = languageStory(
  "sdl",
  sdlSample,
  'filename="schema.graphql"',
  { name: "SDL" }
);

export const HTTP: Story = languageStory("http", httpSample, undefined, {
  name: "HTTP",
});

export const JSON: Story = languageStory(
  "json",
  jsonSample,
  'filename="response.json"',
  { name: "JSON" }
);

export const SQL: Story = languageStory("sql", sqlSample, undefined, {
  name: "SQL",
});

export const Html: Story = languageStory(
  "html",
  htmlSample,
  'filename="index.html"',
  { name: "HTML" }
);

export const Css: Story = languageStory(
  "css",
  cssSample,
  'filename="styles.css"',
  { name: "CSS" }
);

export const XML: Story = languageStory(
  "xml",
  xmlSample,
  'filename="library.xml"',
  { name: "XML" }
);

export const Diff: Story = languageStory("diff", diffSample);

export const PlainText: Story = languageStory(
  "text",
  "Just some plain text. No syntax highlighting applied.",
  undefined,
  { name: "Plain Text" }
);

export const Mermaid: Story = languageStory("mermaid", mermaidSample);
