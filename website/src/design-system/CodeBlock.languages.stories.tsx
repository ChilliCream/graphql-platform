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

const samples: ReadonlyArray<{
  language: string;
  code: string;
  meta?: string;
}> = [
  {
    language: "tsx",
    code: `export const Counter = () => <button>Clicked</button>;`,
    meta: 'filename="Counter.tsx"',
  },
  {
    language: "jsx",
    code: `export const Counter = () => <button>Clicked</button>;`,
    meta: 'filename="Counter.jsx"',
  },
  {
    language: "csharp",
    code: `public sealed record Book(string Title);`,
    meta: 'filename="Book.cs"',
  },
  { language: "bash", code: `dotnet add package HotChocolate.AspNetCore` },
  {
    language: "graphql",
    code: `query { book { title } }`,
    meta: 'filename="GetBook.graphql"',
  },
  {
    language: "sdl",
    code: `type Book { title: String! }`,
    meta: 'filename="schema.graphql"',
  },
  {
    language: "http",
    code: `POST /graphql HTTP/1.1\nContent-Type: application/json`,
  },
  {
    language: "json",
    code: `{ "data": { "book": { "title": "C# in Depth" } } }`,
    meta: 'filename="response.json"',
  },
  { language: "sql", code: `SELECT title FROM books LIMIT 10;` },
  {
    language: "html",
    code: `<h1 class="library">Library</h1>`,
    meta: 'filename="index.html"',
  },
  {
    language: "css",
    code: `.library { display: grid; }`,
    meta: 'filename="styles.css"',
  },
  {
    language: "xml",
    code: `<book id="42">C# in Depth</book>`,
    meta: 'filename="library.xml"',
  },
  { language: "diff", code: `-old line\n+new line` },
  { language: "text", code: `Just some plain text. No highlighting applied.` },
];

export const AllLanguages: Story = {
  loaders: [
    async () => ({
      blocks: await Promise.all(
        samples.map(({ language, code, meta }) =>
          renderBlock(language, code, meta),
        ),
      ),
    }),
  ],
  render: (_args, ctx) => (
    <div className="flex flex-col gap-4">
      {(ctx.loaded.blocks as ReactElement[]).map((block, index) => (
        <div key={samples[index].language}>{block}</div>
      ))}
    </div>
  ),
};
