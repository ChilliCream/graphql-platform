# website

The ChilliCream website and documentation, built on Next.js (MDX-based docs).

> [!NOTE]
> This is **not** the Next.js you may know from training data. APIs, conventions,
> and file structure may differ. Read the relevant guide in
> `node_modules/next/dist/docs/` before writing app/build code.

## Development

Use `yarn` (not `npm`):

```bash
yarn
yarn dev
```

## Authoring Markdown Content

Docs live in `content/docs/<product>/...` as Markdown (`.md`) files and are
compiled with a custom MDX pipeline (`src/mdx-plugins.ts`). The rules below are
specific to this repo. Following them keeps the build green (several of these are
enforced at build time and will fail the build if violated).

### File layout

```
content/docs/<product>/<section>/<page>.md
content/docs/<product>/<section>/structure.yaml   # sidebar for that directory
content/docs/<product>/index.md                   # the product/section landing page
```

A directory's `index.md` is its landing page. Images do **not** live next to docs
(see [Images](#images)).

### Frontmatter

Every doc starts with YAML frontmatter:

```markdown
---
title: "Page Title"
description: "One-sentence summary used for SEO and Open Graph."
---
```

- `title` is rendered by the page layout as the page's single `<h1>`.
- **Do not repeat the title as the first heading in the body.** The layout already
  shows it, so a `# Page Title` at the top produces a duplicate heading.
- Headings are **automatically demoted one level** at build time
  (`src/remark/demoteHeadings.mjs`): write `#` in source and it renders as `<h2>`,
  `##` renders as `<h3>`, and so on. So author your top-level sections with `#`.

### Sidebar (`structure.yaml`)

The sidebar is built by walking the directory tree
(`src/helpers/buildContentTree.ts`). **Every directory that appears in the
navigation must contain a `structure.yaml`.**

```yaml
title: Section Title
items:
  - path: index # resolves to ./index.md, used as the section landing page
    title: Overview
  - path: getting-started # resolves to ./getting-started.md
    title: Getting Started
  - path: guides # a subdirectory -> must have its own structure.yaml
    title: Guides
```

- `items` is a **flat** list for the current directory only. Nesting is expressed
  through subdirectories, each with its own `structure.yaml`.
- A `path` resolves to `<path>.md` if that file exists, otherwise to the
  subdirectory `<path>/` (which must contain `index.md` to be linkable and a
  `structure.yaml`).
- A referenced item that doesn't exist on disk fails the build.

### Links

Link to other docs and blog pages directly by their target Markdown file using a
**relative filesystem path**. The `rewriteMdLinks` remark plugin rewrites these to
the correct route at build time, and **fails the build on broken links**.

```markdown
[Sibling page](./other-page.md)
[Sibling blog post](./2026-05-11-hot-chocolate-16.md)
[Page in another section](../guides/first-party-api.md)
[Cross-product page](../../hotchocolate/index.md)
[Deep link with anchor](./other-page.md#a-section)
[Same-page anchor](#a-section) <!-- just the hash, no file -->
```

- Always point at the `.md` file (e.g. `./cli.md`, not `/docs/.../cli`; and
  `./2026-05-11-hot-chocolate-16.md`, not `/blog/2026/05/11/hot-chocolate-16`).
- For a directory/section landing page, link to its `index.md`
  (e.g. `./guides/index.md`).
- Same-page anchors are just `#anchor` (no file path).
- Links to **product/marketing pages** (which have no Markdown source) use a
  **relative root-relative path**, e.g. `/products/nitro`, not an absolute
  `https://chillicream.com/products/nitro` URL.

### Images

Store images under a product namespace in `public/`, **not** next to the docs:

```
public/images/<product>-docs/<name>.webp
```

Reference them with a relative path that resolves **into `public/`**; the
`rewriteMdLinks` plugin rewrites it to a rooted URL (`/images/...`) at build time:

```markdown
![Alt text](../../../public/images/fusion-docs/overview.webp)
```

(Use as many `../` as needed to reach the repo root from the doc's directory.) The
build fails if the referenced file does not exist.

### YouTube videos

Put a YouTube link **alone in its own paragraph**. The `youtubeEmbed` remark
plugin converts it into an embedded player; the link text becomes the play-button
label. A raw Markdown viewer (e.g. GitHub) still shows a clickable link.

```markdown
[Watch the video on YouTube](https://www.youtube.com/watch?v=VIDEO_ID)
```

A YouTube link inside surrounding prose is left as a normal inline link.

### Version badges

Put a `key: value` version line **alone in the paragraph directly below a
heading** to render version badges next to that heading. The `headingTags`
remark plugin picks it up; raw Markdown viewers still show a readable line.

```markdown
# `nitro fusion publish`

Since: 16.6.0, Nitro: 10.3.0
```

Supported keys (any subset, in any order, always rendered in the same order):

| Key     | Badge           | Meaning                                                                     |
| ------- | --------------- | --------------------------------------------------------------------------- |
| `Since` | `16.6.0+`       | Minimum package or tool version required for the documented feature.        |
| `Nitro` | `Nitro 10.3.0+` | Minimum self-hosted Nitro backend version (hovering the badge explains it). |

A paragraph is only converted when every pair uses a supported key, so ordinary
prose such as `Note: this is fine, really` is left alone.

The same line as the **first block of a document** (directly below the
frontmatter) puts the badges on the page title instead:

```markdown
---
title: "fusion Command"
---

Since: 16.6.0
```

### Admonitions

Use GitHub-style alert blockquotes. Supported kinds: `NOTE`, `TIP`, `WARNING`,
`CAUTION`, `EXPERIMENTAL`.

```markdown
> [!WARNING]
> This action cannot be undone.
```

### Diagrams (Mermaid)

Prefer Mermaid over a static image for diagrams (flowcharts, sequence diagrams).
Use a fenced ```mermaid block. Styling (colors, rounded nodes, dimmed lines) is
applied automatically via the theme in `src/mdx-plugins.ts`, so don't hard-code
colors.

````markdown
```mermaid
flowchart LR
    Client --> Gateway["Fusion Gateway"]
    Gateway --> Service["Products Service"]
```
````

> [!IMPORTANT]
> Keep node labels on a **single line**. Multi-line (`<br/>`) or auto-wrapped
> labels are mis-measured during headless rendering and get clipped at the box
> edge. If a label is long, keep it concise rather than wrapping it.

### MDX gotchas

- **Escape literal curly braces.** Raw `{...}` is parsed as a JavaScript
  expression and breaks the build (e.g. format specifiers, placeholders). Write
  `\{` and `\}`, or wrap the value in backticks.
- HTML comments (`<!-- ... -->`) are stripped before compilation; don't rely on
  them rendering.
- A range of MDX components is available (e.g. `Tabs`, `ExampleTabs`,
  `PackageInstallation`); see `mdx-components.tsx` for the full set.

### Learn catalog links

`src/data/learn/*.ts` is the seed data behind `/learn` (templates, tutorials,
examples, workshops, videos). Every external URL in that directory, whether in
a link field (`githubUrl`, `demoUrl`, `externalUrl`) or embedded in a `body`
paragraph or `cli` code string, must point at something that actually exists.

Run `yarn check:learn-links` before adding or editing a learn entry. It
extracts every `http(s)` URL from `src/data/learn/*.ts`, checks `github.com`
repo/tree/blob URLs via `gh api` (the repo, and the path at the ref named in
the URL) and every other host with an HTTP request, and fails with a list of
the broken URLs (and the file/line each one came from) if anything doesn't
resolve. It requires the `gh` CLI to be installed and authenticated.

A URL that legitimately can't be checked from CI (for example a `localhost`
example in tutorial prose) can be added to
`scripts/check-learn-links.allowlist.json`, a JSON array of
`{ "url": "<exact URL as it appears in source>", "reason": "<why>" }`. Use it
only for URLs that are genuinely unreachable from a CI runner, never to
silence a dead link.

This check is not currently wired into CI.
