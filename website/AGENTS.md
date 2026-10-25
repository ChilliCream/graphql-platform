<!-- BEGIN:nextjs-agent-rules -->

# This is NOT the Next.js you know

This version has breaking changes — APIs, conventions, and file structure may all differ from your training data. Read the relevant guide in `node_modules/next/dist/docs/` before writing any code. Heed deprecation notices.

<!-- END:nextjs-agent-rules -->

# Authoring markdown content

When creating or editing content under `content/docs/` or `content/blog/`, follow
the conventions in [README.md → Authoring Markdown Content](./README.md#authoring-markdown-content).
It covers frontmatter, `structure.yaml` sidebars, links, images, YouTube videos,
admonitions, Mermaid diagrams, and MDX gotchas.

# Components

This site is built to be extended agent-first: adding a page or component should
be mechanical and consistent. Keep the quality bar high; do not cut corners
because an agent is doing the work.

## Where code lives

- **Pages**: `app/` route segments. Content/marketing routes live under
  `app/(content)/`. Keep page files thin: compose section components, do not
  inline large markup.
- **Reusable components**: `src/components/`, one component per file
  (PascalCase filename).
- **Design-system primitives** (Button, Input, Image, Typography, ...):
  `src/design-system/`.
- **Icons and inline SVG art**: `src/icons/`, one per file.
- A component that is only a child of one parent and is not reused elsewhere may
  live in the parent's file. As soon as it is reused or is independently
  meaningful, move it to its own file under `src/components/`.

## Props

- Declare props as an `interface` named `<Component>Props`.
- Every property is `readonly`.
- Do **not** export the props interface; it is the component's own
  implementation detail.

```tsx
interface ButtonProps {
  readonly label: string;
  readonly onClick?: () => void;
}

export function Button({ label, onClick }: ButtonProps) {
  // ...
}
```

## SVG and icons

- Add SVGs as inline React components in `src/icons/` that return `<svg>…</svg>`
  directly, so the markup is inlined into the HTML.
- Do **not** add raw `.svg` files to `public/` to load via `<img>` / `Image`:
  the image pipeline does not process SVG, and inlining lets icons inherit
  `currentColor` and be sized/positioned with CSS.
- Accept `className` (and `style` when the caller positions the icon). Default
  decorative icons to `aria-hidden="true"`.
- If the SVG uses element ids (e.g. gradient ids), prefix them per icon so
  several inlined icons on the same page do not collide.

## Typography

Use the type scale from `app/globals.css` (`@theme`): the `text-hero` /
`text-h1`…`text-h6` / `text-lead` / `text-body` / `text-caption` utilities (each
ships its line-height), the `font-heading` / `font-body` families (Tailwind
`font-sans` maps to the body voice), and the `.hero` / `.lead` / `.caption`
helper classes. Avoid ad-hoc `text-4xl`-style sizes for display headings.

## Before you finish

- Verify visually against the running dev server (`yarn dev`).
- `npx eslint <changed files>` must pass.
- `yarn format` must be run and `yarn format:check` must pass (Prettier
  formatting, including Tailwind class sorting, is enforced in CI).
