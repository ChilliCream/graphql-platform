import type { ReactElement } from "react";
import { CodeBlock } from "./CodeBlock";

// CodeBlock is an async server component (uses shiki's `codeToHtml`).
// Storybook's renderer doesn't render async components on the client, so
// each story pre-resolves its JSX in a `loaders` hook and renders the
// captured node synchronously.
export async function renderBlock(
  language: string,
  code: string,
  meta?: string
): Promise<ReactElement> {
  return (await CodeBlock({
    children: (
      <code className={`language-${language}`} data-meta={meta}>
        {code}
      </code>
    ),
  })) as ReactElement;
}
