import type { ReactNode } from "react";
import Link from "next/link";
import { BlogMetadata } from "@/src/components/BlogMetadata";
import { BlogShareBar } from "@/src/components/BlogShareBar";
import { BlogTags } from "@/src/components/BlogTags";
import { TableOfContents, type HeadingItem } from "@/src/components/TableOfContents";
import type { LearnContentType } from "@/src/data/learn/facets";
import { Picture } from "@/src/design-system/Picture";
import { ContentTypeBadge } from "./ContentTypeBadge";

export interface ArticleBreadcrumbItem {
  readonly label: string;
  readonly href?: string;
}

interface ArticleMeta {
  readonly author?: string;
  readonly authorUrl?: string;
  readonly authorImageUrl?: string;
  /** Formatted for display; the shell renders it as-is (no date parsing). */
  readonly publishedDate?: string;
  /** Optional evergreen update date; rendered as an "Updated {date}" line in the kind-chip row. */
  readonly updatedDate?: string;
  readonly readingTime?: string;
}

interface ArticleLayoutProps {
  readonly breadcrumb: readonly ArticleBreadcrumbItem[];
  /** Kind chip shown for comparisons/explainers; omitted for blog posts (section 4.1 item 2). */
  readonly kind?: Extract<LearnContentType, "comparison" | "explainer">;
  readonly title: string;
  /** One-paragraph answer-first summary. Sourced from frontmatter `description`; blog posts omit it. */
  readonly standfirst?: string;
  readonly meta: ArticleMeta;
  readonly heroImageSrc?: string | null;
  readonly shareUrl: string;
  readonly toc: readonly HeadingItem[];
  /** Tag links (section 4.1 item 7), reused as-is via `BlogTags`; targets `/learn/articles/tags/[tag]`. */
  readonly tags?: readonly string[];
  readonly children: ReactNode;
  /** Related-items slot: `SimilarPosts` for blog, a `CardGrid` for comparisons/explainers (section 4.1 item 9). */
  readonly related?: ReactNode;
}

/**
 * Presentational article reading shell shared by blog posts, comparisons,
 * and explainers (learn-editorial.md section 4.1, amended by
 * learn-harmonization.md D5/D6 and superseded by website-kbx.18). Takes
 * plain props only: no blog imports, no filesystem reads.
 *
 * Header, body, and `related` all span the full `1fr` main column of the
 * shared `[1fr_20rem]` grid (the TOC rail keeps its `20rem` track where it
 * renders); no article-shell or reading-column width cap is applied
 * (website-kbx.18, superseding the `max-w-5xl` shell / `max-w-2xl` reading
 * column of website-kbx.15 and the `max-w-5xl` header / `max-w-[46rem]`
 * prose split of website-kbx.7 and learn-harmonization.md D5). The hero
 * image is the one deliberate exception: pending Pascal's design call on the
 * kbx.18 crop review, it keeps a `max-w-3xl` width cap (the kbx.7 treatment)
 * instead of spanning the full column, so no hero art is cropped. See
 * learn-editorial.md section 4.1's kbx.18 amendment for the rationale.
 */
export function ArticleLayout({
  breadcrumb,
  kind,
  title,
  standfirst,
  meta,
  heroImageSrc,
  shareUrl,
  toc,
  tags,
  children,
  related,
}: ArticleLayoutProps) {
  return (
    <div className="grid grid-cols-1 2xl:grid-cols-[1fr_20rem]">
      <main className="min-w-0">
        <article>
          <ArticleBreadcrumb items={breadcrumb} />
          {kind ? (
            <div className="mt-3 flex flex-wrap items-center gap-3">
              <ContentTypeBadge type={kind} />
              {meta.updatedDate && meta.updatedDate !== meta.publishedDate ? (
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
              sizes="(max-width: 639px) calc(100vw - 2.5rem), (max-width: 1535px) calc(100vw - 6rem), min(calc(100vw - 6rem - 20rem), 48rem)"
              className="mt-6 mb-6 aspect-video w-full max-w-3xl rounded-lg"
            />
          ) : null}
          <h1 className="font-heading text-cc-heading text-h3 mt-10 mb-4 font-semibold tracking-[-0.02em] text-balance">
            {title}
          </h1>
          {standfirst ? <p className="text-cc-ink-dim my-4 text-lg leading-relaxed">{standfirst}</p> : null}
          <div className="flex flex-wrap items-center justify-between gap-4">
            <BlogMetadata
              author={meta.author}
              authorUrl={meta.authorUrl}
              authorImageUrl={meta.authorImageUrl}
              date={meta.publishedDate}
              readingTime={meta.readingTime}
            />
            <BlogShareBar url={shareUrl} title={title} />
          </div>
          <BlogTags tags={tags ? [...tags] : undefined} />
          <div className="[&_li[data-prose]]:text-lg [&_li[data-prose]]:leading-8 [&_p[data-prose]]:text-lg [&_p[data-prose]]:leading-8">
            {children}
          </div>
          {related}
        </article>
      </main>
      <TableOfContents items={[...toc]} />
    </div>
  );
}

export function ArticleBreadcrumb({ items }: { readonly items: readonly ArticleBreadcrumbItem[] }) {
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
