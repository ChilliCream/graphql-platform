import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { LearnDetail } from "@/src/components/learn/LearnDetail";
import { productLabel } from "@/src/data/learn/facets";
import { findRelatedCatalogItems, WORKSHOP_ITEMS } from "@/src/data/learn/content";
import type { WorkshopItem } from "@/src/data/learn/types";
import { ORGANIZATION_ID } from "@/src/helpers/structuredData";
import { pageMetadata } from "@/src/helpers/pageMetadata";
import { SITE_URL } from "@/src/helpers/siteUrl";

interface PageProps {
  readonly params: Promise<{ readonly slug: string }>;
}

export const dynamicParams = false;

export function generateStaticParams(): { slug: string }[] {
  return WORKSHOP_ITEMS.map((workshop) => ({ slug: workshop.slug }));
}

function findWorkshop(slug: string): WorkshopItem | undefined {
  return WORKSHOP_ITEMS.find((workshop) => workshop.slug === slug);
}

export async function generateMetadata({ params }: PageProps): Promise<Metadata> {
  const { slug } = await params;
  const workshop = findWorkshop(slug);
  if (!workshop) {
    return {};
  }
  return pageMetadata({
    title: workshop.title,
    description: workshop.tagline,
    path: `/learn/workshops/${workshop.slug}`,
    keywords: ["GraphQL workshop", ...workshop.products.map(productLabel)],
  });
}

// A workshop is a hands-on, multi-part curriculum rather than a single
// article, so `Course` (a real schema.org type) fits better than
// `TechArticle`. `provider` mirrors the organization reference every other
// learn detail page's JSON-LD uses for `author`.
const structuredData = (workshop: WorkshopItem) => ({
  "@context": "https://schema.org",
  "@graph": [
    {
      "@type": "BreadcrumbList",
      itemListElement: [
        { "@type": "ListItem", position: 1, name: "Home", item: `${SITE_URL}/` },
        { "@type": "ListItem", position: 2, name: "Learn", item: `${SITE_URL}/learn` },
        { "@type": "ListItem", position: 3, name: workshop.title },
      ],
    },
    {
      "@type": "Course",
      name: workshop.title,
      description: workshop.tagline,
      url: `${SITE_URL}/learn/workshops/${workshop.slug}`,
      provider: { "@id": ORGANIZATION_ID },
    },
  ],
});

export default async function WorkshopPage({ params }: PageProps) {
  const { slug } = await params;
  const workshop = findWorkshop(slug);
  if (!workshop) {
    notFound();
  }
  const related = findRelatedCatalogItems(workshop);
  return (
    <>
      <script
        type="application/ld+json"
        dangerouslySetInnerHTML={{ __html: JSON.stringify(structuredData(workshop)) }}
      />
      <LearnDetail item={workshop} related={related} />
    </>
  );
}
