import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { LearnDetail } from "@/src/components/learn/LearnDetail";
import { productLabel } from "@/src/data/learn/facets";
import { EXAMPLE_ITEMS, findRelatedCatalogItems } from "@/src/data/learn/content";
import type { ExampleItem } from "@/src/data/learn/types";
import { ORGANIZATION_ID } from "@/src/helpers/structuredData";
import { pageMetadata } from "@/src/helpers/pageMetadata";
import { SITE_URL } from "@/src/helpers/siteUrl";

interface PageProps {
  readonly params: Promise<{ readonly slug: string }>;
}

export const dynamicParams = false;

export function generateStaticParams(): { slug: string }[] {
  return EXAMPLE_ITEMS.map((example) => ({ slug: example.slug }));
}

function findExample(slug: string): ExampleItem | undefined {
  return EXAMPLE_ITEMS.find((example) => example.slug === slug);
}

export async function generateMetadata({ params }: PageProps): Promise<Metadata> {
  const { slug } = await params;
  const example = findExample(slug);
  if (!example) {
    return {};
  }
  return pageMetadata({
    title: example.title,
    description: example.tagline,
    path: `/learn/examples/${example.slug}`,
    keywords: ["GraphQL example", ...example.products.map(productLabel)],
  });
}

// Examples are runnable repos, so `SoftwareSourceCode` (per the ticket) fits
// directly, matching the template page's own JSON-LD shape. `codeRepository`
// is only emitted when the example carries a `githubUrl`: the field is
// optional on `ExampleItem` since a future example could be docs-hosted.
const structuredData = (example: ExampleItem) => ({
  "@context": "https://schema.org",
  "@graph": [
    {
      "@type": "BreadcrumbList",
      itemListElement: [
        { "@type": "ListItem", position: 1, name: "Home", item: `${SITE_URL}/` },
        { "@type": "ListItem", position: 2, name: "Learn", item: `${SITE_URL}/learn` },
        { "@type": "ListItem", position: 3, name: example.title },
      ],
    },
    {
      "@type": "SoftwareSourceCode",
      name: example.title,
      description: example.tagline,
      url: `${SITE_URL}/learn/examples/${example.slug}`,
      ...(example.githubUrl ? { codeRepository: example.githubUrl } : {}),
      author: { "@id": ORGANIZATION_ID },
    },
  ],
});

export default async function ExamplePage({ params }: PageProps) {
  const { slug } = await params;
  const example = findExample(slug);
  if (!example) {
    notFound();
  }
  const related = findRelatedCatalogItems(example);
  return (
    <>
      <script
        type="application/ld+json"
        dangerouslySetInnerHTML={{ __html: JSON.stringify(structuredData(example)) }}
      />
      <LearnDetail item={example} related={related} />
    </>
  );
}
