import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { LearnDetail } from "@/src/components/learn/LearnDetail";
import { productLabel } from "@/src/data/learn/facets";
import { findRelatedCatalogItems, TUTORIAL_ITEMS } from "@/src/data/learn/content";
import type { TutorialItem } from "@/src/data/learn/types";
import { ORGANIZATION_ID } from "@/src/helpers/structuredData";
import { pageMetadata } from "@/src/helpers/pageMetadata";
import { SITE_URL } from "@/src/helpers/siteUrl";

interface PageProps {
  readonly params: Promise<{ readonly slug: string }>;
}

export const dynamicParams = false;

export function generateStaticParams(): { slug: string }[] {
  return TUTORIAL_ITEMS.map((tutorial) => ({ slug: tutorial.slug }));
}

function findTutorial(slug: string): TutorialItem | undefined {
  return TUTORIAL_ITEMS.find((tutorial) => tutorial.slug === slug);
}

export async function generateMetadata({ params }: PageProps): Promise<Metadata> {
  const { slug } = await params;
  const tutorial = findTutorial(slug);
  if (!tutorial) {
    return {};
  }
  return pageMetadata({
    title: tutorial.title,
    description: tutorial.tagline,
    path: `/learn/tutorials/${tutorial.slug}`,
    keywords: ["GraphQL tutorial", ...tutorial.products.map(productLabel)],
  });
}

// Tutorials are walkthroughs, not code deliverables, so `TechArticle` (a
// schema.org `Article` subtype for technical/how-to writing) fits better than
// `SoftwareSourceCode`. No `datePublished`/`dateModified`: the seed data only
// carries a relative freshness string (`updatedRelative`), not a real date.
const structuredData = (tutorial: TutorialItem) => ({
  "@context": "https://schema.org",
  "@graph": [
    {
      "@type": "BreadcrumbList",
      itemListElement: [
        { "@type": "ListItem", position: 1, name: "Home", item: `${SITE_URL}/` },
        { "@type": "ListItem", position: 2, name: "Learn", item: `${SITE_URL}/learn` },
        { "@type": "ListItem", position: 3, name: tutorial.title },
      ],
    },
    {
      "@type": "TechArticle",
      headline: tutorial.title,
      name: tutorial.title,
      description: tutorial.tagline,
      url: `${SITE_URL}/learn/tutorials/${tutorial.slug}`,
      author: { "@id": ORGANIZATION_ID },
    },
  ],
});

export default async function TutorialPage({ params }: PageProps) {
  const { slug } = await params;
  const tutorial = findTutorial(slug);
  if (!tutorial) {
    notFound();
  }
  const related = findRelatedCatalogItems(tutorial);
  return (
    <>
      <script
        type="application/ld+json"
        dangerouslySetInnerHTML={{ __html: JSON.stringify(structuredData(tutorial)) }}
      />
      <LearnDetail item={tutorial} related={related} />
    </>
  );
}
