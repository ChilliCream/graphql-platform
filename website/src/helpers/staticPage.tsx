import fs from "node:fs";
import path from "node:path";
import type { Metadata } from "next";
import { PageStructuredData } from "@/src/components/PageStructuredData";
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
    const title = frontmatter.title ?? "";
    const description = frontmatter.description;
    const dateModified = readVisibleModifiedDate(absPath);

    return (
      <>
        {title ? (
          <PageStructuredData
            title={title}
            description={description}
            dateModified={dateModified}
            path={pagePath}
            breadcrumbs={[{ name: "Home", path: "/" }, { name: title }]}
          />
        ) : null}
        {title ? <Typography variant="h1">{title}</Typography> : null}
        {content}
      </>
    );
  }

  return { generateMetadata, Page };
}

function readVisibleModifiedDate(absPath: string): string | undefined {
  const source = fs.readFileSync(absPath, "utf8");
  const match = /^Last updated: (\d{1,2}) ([A-Za-z]+) (\d{4})$/m.exec(source);
  if (!match) {
    return undefined;
  }

  const [, day, monthName, year] = match;
  const month = MONTHS[monthName];
  if (month === undefined) {
    return undefined;
  }
  return `${year}-${String(month).padStart(2, "0")}-${day.padStart(2, "0")}`;
}

const MONTHS: Readonly<Record<string, number>> = {
  January: 1,
  February: 2,
  March: 3,
  April: 4,
  May: 5,
  June: 6,
  July: 7,
  August: 8,
  September: 9,
  October: 10,
  November: 11,
  December: 12,
};
