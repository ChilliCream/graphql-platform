import type { ReactNode } from "react";
import { BlogTeaserGrid } from "@/src/components/BlogTeaserGrid";
import type { BlogTeaserData } from "@/src/components/BlogTeaser";
import { PageSection } from "@/src/components/PageSection";
import { Pagination } from "@/src/design-system/Pagination";
import { Typography } from "@/src/design-system/Typography";

interface BlogIndexShellPagination {
  readonly currentPage: number;
  readonly totalPages: number;
  readonly hrefForPage: (page: number) => string;
}

interface BlogIndexShellProps {
  readonly title: string;
  /**
   * Extra content rendered under the title, e.g. a post-count line. When
   * provided, the title and subtitle are wrapped in a shared header block.
   */
  readonly subtitle?: ReactNode;
  readonly posts: BlogTeaserData[];
  /** Omit to render the grid without pagination, e.g. an empty-state page. */
  readonly pagination?: BlogIndexShellPagination;
}

export function BlogIndexShell({
  title,
  subtitle,
  posts,
  pagination,
}: BlogIndexShellProps) {
  return (
    <PageSection maxWidth="7xl" className="flex flex-col gap-6 py-8">
      {subtitle ? (
        <header className="flex flex-col gap-1">
          <Typography variant="h1">{title}</Typography>
          {subtitle}
        </header>
      ) : (
        <Typography variant="h1">{title}</Typography>
      )}
      <BlogTeaserGrid posts={posts} />
      {pagination ? (
        <Pagination
          currentPage={pagination.currentPage}
          totalPages={pagination.totalPages}
          hrefForPage={pagination.hrefForPage}
        />
      ) : null}
    </PageSection>
  );
}
