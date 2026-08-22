import type { ReactNode } from "react";
import Link from "next/link";
import { BlogMetadata } from "@/src/components/BlogMetadata";
import { BlogShareBar } from "@/src/components/BlogShareBar";
import { BlogTags } from "@/src/components/BlogTags";
import { TableOfContents, type HeadingItem } from "@/src/components/TableOfContents";
import type { LearnContentType } from "@/src/data/learn/facets";
import { Picture } from "@/src/design-system/Picture";
import { Typography } from "@/src/design-system/Typography";
import { ContentTypeBadge } from "./ContentTypeBadge";

interface ArticleBreadcrumbItem {
  readonly label: string;
  readonly href?: string;
}

interface ArticleMeta {
  readonly author?: string;
  readonly authorUrl?: string;
  readonly authorImageUrl?: string;
  /** Formatted for display; the shell renders it as-is (no date parsing). */
  readonly publishedDate?: string;
  /** Present only for evergreen kinds (comparison/explainer); rendered in place of `publishedDate` per section 4.1. */
  readonly updatedDate?: string;
  readonly readingTime?: string;
}

interface ArticleLayoutProps {
  readonly breadcrumb: readonly ArticleBreadcrumbItem[];
  /** Kind chip shown for comparisons/explainers; omitted for blog posts (section 4.1 item 2). */
  readonly kind?: Extract<LearnContentType, "article" | "comparison" | "explainer">;
  readonly title: string;
  /** One-paragraph answer-first summary. Sourced from frontmatter `description`; blog posts omit it. */
  readonly standfirst?: string;
  readonly meta: ArticleMeta;
  readonly heroImageSrc?: string | null;
  readonly shareUrl: string;
  readonly tags?: readonly string[];
  readonly toc: readonly HeadingItem[];
  readonly children: ReactNode;
  /** Related-items slot: `SimilarPosts` for blog, a `CardGrid` for comparisons/explainers (section 4.1 item 9). */
  readonly related?: ReactNode;
}

/**
 * Presentational article reading shell shared by blog posts, comparisons,
 * and explainers (learn-editorial.md section 4.1). Takes plain props only:
 * no blog imports, no filesystem reads.
 */
export function ArticleLayout({
  breadcrumb,
  kind,
  title,
  standfirst,
  meta,
  heroImageSrc,
  shareUrl,
  tags,
  toc,
  children,
  related,
}: ArticleLayoutProps) {
  return (
    <div className="grid grid-cols-1 2xl:grid-cols-[1fr_20rem]">
      <main className="min-w-0">
        <article className="mx-auto max-w-5xl">
          <ArticleBreadcrumb items={breadcrumb} />
          {kind ? (
            <div className="mt-3 flex flex-wrap items-center gap-3">
              <ContentTypeBadge type={kind} />
              {meta.updatedDate ? (
                <span className="text-cc-ink-dim font-mono text-xs tracking-wider uppercase">
                  Updated {meta.updatedDate}
                </span>
              ) : null}
            </div>
          ) : null}
          {heroImageSrc ? (
            <Picture
              src={heroImageSrc}
              alt=""
              priority
              sizes="(max-width: 639px) calc(100vw - 2.5rem), (max-width: 1119px) calc(100vw - 6rem), 1024px"
              className="mt-6 mb-6 aspect-video w-full rounded-lg object-cover"
            />
          ) : null}
          <Typography variant="h1">{title}</Typography>
          {standfirst ? <p className="text-cc-ink-dim my-4 text-lg leading-relaxed">{standfirst}</p> : null}
          <div className="flex flex-wrap items-center justify-between gap-4">
            <BlogMetadata
              author={meta.author}
              authorUrl={meta.authorUrl}
              authorImageUrl={meta.authorImageUrl}
              date={meta.updatedDate ?? meta.publishedDate}
              readingTime={meta.readingTime}
            />
            <BlogShareBar url={shareUrl} title={title} />
          </div>
          <BlogTags tags={tags ? [...tags] : undefined} />
          {children}
          {related}
        </article>
      </main>
      <TableOfContents items={[...toc]} />
    </div>
  );
}

function ArticleBreadcrumb({ items }: { readonly items: readonly ArticleBreadcrumbItem[] }) {
  if (items.length === 0) {
    return null;
  }
  return (
    <nav aria-label="Breadcrumb" className="font-mono text-xs tracking-wider uppercase">
      <ol className="text-cc-ink-dim m-0 flex list-none flex-wrap items-center gap-x-2 p-0">
        {items.map((item, index) => (
          <li key={`${item.label}-${index}`} className="flex items-center gap-x-2">
            {index > 0 ? <span aria-hidden="true">/</span> : null}
            {item.href ? (
              <Link href={item.href} className="hover:text-cc-accent transition-colors">
                {item.label}
              </Link>
            ) : (
              <span>{item.label}</span>
            )}
          </li>
        ))}
      </ol>
    </nav>
  );
}
