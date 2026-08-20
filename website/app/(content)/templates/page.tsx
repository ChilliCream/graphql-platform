import { Suspense } from "react";
import { BlueprintBackdrop } from "@/src/components/templates/BlueprintBackdrop";
import { TemplateCatalog } from "@/src/components/templates/TemplateCatalog";
import { TemplatesClosing } from "@/src/components/templates/TemplatesClosing";
import { TemplatesHero } from "@/src/components/templates/TemplatesHero";
import { TEMPLATE_SUMMARIES } from "@/src/data/templates/templates";
import { pageMetadata } from "@/src/helpers/pageMetadata";
import { SITE_URL } from "@/src/helpers/siteUrl";

export const metadata = pageMetadata({
  title: "Templates",
  description: "Production-ready GraphQL services, federations, and clients. Clone, customize, and ship.",
  path: "/templates",
  keywords: ["GraphQL templates", "Hot Chocolate", "Fusion", ".NET"],
});

// Flip to hide the faint generated blueprint sheet behind the page.
const SHOW_BLUEPRINT_BACKDROP = true;

const STRUCTURED_DATA = {
  "@context": "https://schema.org",
  "@graph": [
    {
      "@type": "BreadcrumbList",
      itemListElement: [
        { "@type": "ListItem", position: 1, name: "Home", item: `${SITE_URL}/` },
        { "@type": "ListItem", position: 2, name: "Templates" },
      ],
    },
    {
      "@type": "ItemList",
      name: "ChilliCream GraphQL Templates",
      numberOfItems: TEMPLATE_SUMMARIES.length,
      itemListElement: TEMPLATE_SUMMARIES.map((template, index) => ({
        "@type": "ListItem",
        position: index + 1,
        name: template.title,
        url: `${SITE_URL}/templates/${template.slug}`,
      })),
    },
  ],
};

export default function TemplatesPage() {
  return (
    <div className="relative isolate">
      <script type="application/ld+json" dangerouslySetInnerHTML={{ __html: JSON.stringify(STRUCTURED_DATA) }} />
      {SHOW_BLUEPRINT_BACKDROP && <BlueprintBackdrop className="text-cc-accent -z-10 opacity-45 max-lg:hidden" />}
      <TemplatesHero />
      <Suspense fallback={<CatalogFallback />}>
        <TemplateCatalog templates={TEMPLATE_SUMMARIES} />
      </Suspense>
      <TemplatesClosing />
    </div>
  );
}

function CatalogFallback() {
  return <div className="min-h-[48rem] py-14 sm:py-20" aria-hidden="true" />;
}
