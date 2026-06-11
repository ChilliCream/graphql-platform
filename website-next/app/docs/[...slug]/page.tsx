import path from "node:path";
import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { DocPageMeta } from "@/src/components/DocPageMeta";
import { EditOnGitHub } from "@/src/components/EditOnGitHub";
import { TableOfContents } from "@/src/components/TableOfContents";
import { Typography } from "@/src/design-system/Typography";
import { NotFoundContent } from "@/src/components/NotFoundContent";
import { docBreadcrumbs } from "@/src/helpers/buildContentTree";
import { compileDoc } from "@/src/helpers/compileDoc";
import {
  CONTENT_ROOT,
  encodeDocId,
  listDocProducts,
  listDocSlugs,
  NOT_FOUND_SEGMENT,
  resolveFile,
} from "@/src/helpers/docsParams";
import { getGitMetadata } from "@/src/helpers/gitMetadata";
import { githubEditUrl } from "@/src/helpers/githubEditUrl";
import { readFrontmatter } from "@/src/helpers/readFrontmatter";
import { toAbsoluteUrl } from "@/src/helpers/siteUrl";

type Params = {
  slug: string[];
};

type PageProps = {
  params: Promise<Params>;
};

export const dynamicParams = false;

export function generateStaticParams(): Params[] {
  const docs = listDocSlugs().map((slug) => ({ slug }));

  // Static 404 pages: one per product plus a docs-level fallback. nginx serves
  // the closest one for unmatched docs URLs so the secondary link is in the HTML.
  const notFound: Params[] = [
    { slug: [NOT_FOUND_SEGMENT] },
    ...listDocProducts().map((product) => ({
      slug: [product, NOT_FOUND_SEGMENT],
    })),
  ];

  return [...docs, ...notFound];
}

/** Builds the secondary link for a 404 slug, or `null` if it is not one. */
function notFoundSecondary(
  slug: string[],
): { href: string; label: string } | null {
  if (slug[slug.length - 1] !== NOT_FOUND_SEGMENT) {
    return null;
  }
  if (slug.length > 1) {
    const product = slug[0];
    return { href: `/docs/${product}`, label: "Read the docs" };
  }
  return { href: "/docs", label: "Browse the docs" };
}

export async function generateMetadata({
  params,
}: PageProps): Promise<Metadata> {
  const { slug } = await params;
  if (notFoundSecondary(slug) !== null) {
    return { title: "Page not found", robots: { index: false, follow: false } };
  }
  const rel = resolveFile(slug);
  if (rel === null) {
    return {};
  }
  const { title, description } = readFrontmatter(path.join(CONTENT_ROOT, rel));
  const gitMeta = await getGitMetadata(path.join(CONTENT_ROOT, rel));

  // Surface the product in the title tag ("OpenAPI Adapter - Hot Chocolate"),
  // since searches almost always include the product name. Skip the suffix on
  // product index pages, where the title already is the product name.
  const product = docBreadcrumbs(slug.slice(0, 1))[0]?.name;
  const pageTitle =
    title && product && title !== product ? `${title} - ${product}` : title;

  const canonical = `/docs/${slug.join("/")}`;

  const id = encodeDocId(slug);
  const ogImage = {
    url: toAbsoluteUrl(`/docs-og/${id}/opengraph-image`),
    width: 1200,
    height: 630,
    type: "image/png",
    alt: title ? `${title} documentation` : "ChilliCream documentation",
  };

  return {
    // `absolute` bypasses the layout's "%s - ChilliCream" template; the
    // product suffix replaces it.
    title: pageTitle ? { absolute: pageTitle } : undefined,
    description,
    alternates: {
      canonical,
    },
    openGraph: {
      type: "article",
      title: pageTitle,
      description,
      images: [ogImage],
      url: canonical,
      modifiedTime: gitMeta.isoDate,
    },
    twitter: {
      card: "summary_large_image",
      title: pageTitle,
      description,
      images: [ogImage],
    },
  };
}

export default async function DocPage({ params }: PageProps) {
  const { slug } = await params;

  const secondary = notFoundSecondary(slug);
  if (secondary !== null) {
    return <NotFoundContent secondary={secondary} />;
  }

  const rel = resolveFile(slug);

  if (rel === null) {
    notFound();
  }

  const absolutePath = path.join(CONTENT_ROOT, rel);
  const { content, frontmatter, toc } = await compileDoc(absolutePath);
  const gitMeta = await getGitMetadata(absolutePath);

  const pageHref = `/docs/${slug.join("/")}`;
  const crumbs = [
    { name: "Docs", href: "/docs" },
    ...docBreadcrumbs(slug),
  ].filter((c, i, all) => c.href !== null || i === all.length - 1);
  // Pages that exist on disk but are not (yet) referenced in the navigation
  // tree still get a leaf crumb, named by their frontmatter title.
  if (crumbs[crumbs.length - 1]?.href !== pageHref) {
    crumbs.push({
      name: frontmatter.title ?? slug[slug.length - 1],
      href: pageHref,
    });
  }
  const jsonLd = [
    {
      "@context": "https://schema.org",
      "@type": "TechArticle",
      headline: frontmatter.title,
      ...(frontmatter.description
        ? { description: frontmatter.description }
        : {}),
      dateModified: gitMeta.isoDate,
      mainEntityOfPage: toAbsoluteUrl(`/docs/${slug.join("/")}`),
    },
    {
      "@context": "https://schema.org",
      "@type": "BreadcrumbList",
      itemListElement: crumbs.map((c, i) => ({
        "@type": "ListItem",
        position: i + 1,
        name: c.name,
        ...(c.href && i < crumbs.length - 1
          ? { item: toAbsoluteUrl(c.href) }
          : {}),
      })),
    },
  ];

  return (
    <div className="grid grid-cols-1 2xl:grid-cols-[1fr_20rem]">
      <main className="min-w-0 px-5 py-8 sm:px-12">
        <script
          type="application/ld+json"
          dangerouslySetInnerHTML={{ __html: JSON.stringify(jsonLd) }}
        />
        <article className="mx-auto max-w-5xl">
          {frontmatter.title ? (
            <Typography variant="h1">{frontmatter.title}</Typography>
          ) : null}

          {content}

          <EditOnGitHub href={githubEditUrl(`content/docs/${rel}`)} />

          <DocPageMeta
            isoDate={gitMeta.isoDate}
            displayDate={gitMeta.displayDate}
            author={gitMeta.author}
          />
        </article>
      </main>
      <TableOfContents items={toc} />
    </div>
  );
}
