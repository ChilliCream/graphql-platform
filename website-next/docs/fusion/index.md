---
title: Fusion
description: A showcase of every markdown feature.
---

# Heading Level 1

## Heading Level 2

### Heading Level 3

#### Heading Level 4

##### Heading Level 5

###### Heading Level 6

## Paragraphs and Inline Formatting

This is a regular paragraph with **bold text**, *italic text*, ***bold and italic***, ~~strikethrough~~, and `inline code`. You can also combine **_bold italic_** using underscores. Here is a hard line break:
end of line.

A second paragraph follows after a blank line. Inline HTML is also supported: <kbd>Ctrl</kbd> + <kbd>C</kbd>.

## Links and References

- [Inline link](https://chillicream.com)
- [Link with title](https://chillicream.com "ChilliCream homepage")
- Autolink: <https://chillicream.com>
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

Fenced code block with language:

```ts
import { createServer } from "node:http";

const server = createServer((req, res) => {
  res.writeHead(200, { "Content-Type": "text/plain" });
  res.end("Hello, world!\n");
});

server.listen(3000);
```

```graphql
type Query {
  user(id: ID!): User
}

type User {
  id: ID!
  name: String!
}
```

```bash
yarn install
yarn dev
```

Indented code block:

    plain text
    no syntax highlighting

## Tables

| Feature        | Status      | Notes                  |
| -------------- | :---------: | ---------------------: |
| Headings       | ✅          | All six levels         |
| Tables         | ✅          | With alignment         |
| Footnotes      | ✅          | See bottom of page     |

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
