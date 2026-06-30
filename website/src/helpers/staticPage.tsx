import path from "node:path";
import type { Metadata } from "next";
import { Typography } from "@/src/design-system/Typography";
import { compileDoc } from "@/src/helpers/compileDoc";
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

  async function generateMetadata(): Promise<Metadata> {
    const { title, description } = readFrontmatter(absPath);
    return { title, description };
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
