import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { TemplateDetail } from "@/src/components/learn/TemplateDetail";
import { languageLabel, productLabel } from "@/src/data/learn/facets";
import {
  findRelatedTemplates,
  findTemplate,
  LEARN_SUMMARIES,
  TEMPLATE_ITEMS,
  TEMPLATE_SUMMARIES,
} from "@/src/data/learn/content";
import type { LearnItemSummary, TemplateItem } from "@/src/data/learn/types";
import { pageMetadata } from "@/src/helpers/pageMetadata";
import { SITE_URL } from "@/src/helpers/siteUrl";

interface PageProps {
  readonly params: Promise<{ readonly slug: string }>;
}

/** Related items shown below a template: same-type templates first, then other content types sharing a product. */
const MAX_RELATED = 3;

export const dynamicParams = false;

export function generateStaticParams(): { slug: string }[] {
  return TEMPLATE_ITEMS.map((template) => ({ slug: template.slug }));
}

export async function generateMetadata({ params }: PageProps): Promise<Metadata> {
  const { slug } = await params;
  const template = findTemplate(slug);
  if (!template) {
    return {};
  }
  return pageMetadata({
    title: `${template.title} Template`,
    description: template.tagline,
    path: `/learn/templates/${template.slug}`,
    keywords: ["GraphQL template", ...template.products.map(productLabel)],
  });
}

const structuredData = (template: TemplateItem) => ({
  "@context": "https://schema.org",
  "@graph": [
    {
      "@type": "BreadcrumbList",
      itemListElement: [
        { "@type": "ListItem", position: 1, name: "Home", item: `${SITE_URL}/` },
        { "@type": "ListItem", position: 2, name: "Learn", item: `${SITE_URL}/learn` },
        { "@type": "ListItem", position: 3, name: template.title },
      ],
    },
    {
      "@type": "SoftwareSourceCode",
      name: template.title,
      description: template.tagline,
      url: `${SITE_URL}/learn/templates/${template.slug}`,
      codeRepository: template.githubUrl,
      programmingLanguage: languageLabel(template.language),
      license: template.license,
      author: { "@id": `${SITE_URL}/#organization` },
    },
  ],
});

/**
 * Related items for a template: same-type templates first (topology, then
 * product overlap), then other content types sharing a product, capped at
 * {@link MAX_RELATED}.
 */
function findRelated(template: TemplateItem): readonly LearnItemSummary[] {
  const sameType = findRelatedTemplates(template, MAX_RELATED)
    .map((related) => TEMPLATE_SUMMARIES.find((summary) => summary.slug === related.slug))
    .filter((summary) => summary !== undefined);
  if (sameType.length >= MAX_RELATED) {
    return sameType;
  }
  const usedSlugs = new Set([template.slug, ...sameType.map((summary) => summary.slug)]);
  const otherType = LEARN_SUMMARIES.filter(
    (item) =>
      item.type !== "template" &&
      !usedSlugs.has(item.slug) &&
      item.products.some((product) => template.products.includes(product)),
  );
  return [...sameType, ...otherType].slice(0, MAX_RELATED);
}

export default async function TemplatePage({ params }: PageProps) {
  const { slug } = await params;
  const template = findTemplate(slug);
  if (!template) {
    notFound();
  }
  const related = findRelated(template);
  return (
    <>
      <script
        type="application/ld+json"
        dangerouslySetInnerHTML={{ __html: JSON.stringify(structuredData(template)) }}
      />
      <TemplateDetail template={template} related={related} />
    </>
  );
}
