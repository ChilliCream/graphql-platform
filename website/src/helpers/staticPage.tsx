import path from "node:path";
import type { Metadata } from "next";
import { Typography } from "@/src/design-system/Typography";
import { compileDoc } from "@/src/helpers/compileDoc";
import { pageMetadata } from "@/src/helpers/pageMetadata";
import { readFrontmatter } from "@/src/helpers/readFrontmatter";

/**
 * Wires up a static markdown page: reads frontmatter for the window title and
 * meta description, and compiles the body via `compileDoc`. The frontmatter
 * `title` is rendered as the page-level h1 above the content.
 *
 * `relPath` is resolved from the project's `content/` directory
 * (e.g. `legal/privacy-policy.md`).
 */
export function createStaticPage(relPath: string) {
  const absPath = path.join(process.cwd(), "content", relPath);
  // The content tree mirrors the route tree (minus route groups), so the URL
  // path is the relative content path without its `.md` extension.
  const pagePath = `/${relPath.replace(/\.md$/, "")}`;

  async function generateMetadata(): Promise<Metadata> {
    const { title, description } = readFrontmatter(absPath);
    if (!title || !description) {
      const missing = [!title && "title", !description && "description"]
        .filter(Boolean)
        .join(", ");
      throw new Error(
        `Static page "${relPath}" is missing required frontmatter: ${missing}.`,
      );
    }
    return pageMetadata({ title, description, path: pagePath });
  }

  async function Page() {
    const { content, frontmatter } = await compileDoc(absPath);
    return (
      <>
        {frontmatter.title ? (
          <Typography variant="h1">{frontmatter.title}</Typography>
        ) : null}
        {content}
      </>
    );
  }

  return { generateMetadata, Page };
}
