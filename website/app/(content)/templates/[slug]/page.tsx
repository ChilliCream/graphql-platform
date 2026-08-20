import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { TemplateDetail } from "@/src/components/templates/TemplateDetail";
import { languageLabel, productLabel } from "@/src/data/templates/filters";
import {
  findRelated,
  findTemplate,
  TEMPLATES,
  type Template,
  type TemplateSummary,
} from "@/src/data/templates/templates";
import { pageMetadata } from "@/src/helpers/pageMetadata";
import { SITE_URL } from "@/src/helpers/siteUrl";

interface PageProps {
  readonly params: Promise<{ readonly slug: string }>;
}

export const dynamicParams = false;

export function generateStaticParams(): { slug: string }[] {
  return TEMPLATES.map((template) => ({ slug: template.slug }));
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
    path: `/templates/${template.slug}`,
    keywords: ["GraphQL template", ...template.products.map(productLabel)],
  });
}

const structuredData = (template: Template) => ({
  "@context": "https://schema.org",
  "@graph": [
    {
      "@type": "BreadcrumbList",
      itemListElement: [
        { "@type": "ListItem", position: 1, name: "Home", item: `${SITE_URL}/` },
        { "@type": "ListItem", position: 2, name: "Templates", item: `${SITE_URL}/templates` },
        { "@type": "ListItem", position: 3, name: template.title },
      ],
    },
    {
      "@type": "SoftwareSourceCode",
      name: template.title,
      description: template.tagline,
      url: `${SITE_URL}/templates/${template.slug}`,
      codeRepository: template.githubUrl,
      programmingLanguage: languageLabel(template.language),
      license: template.license,
      author: { "@id": `${SITE_URL}/#organization` },
    },
  ],
});

export default async function TemplatePage({ params }: PageProps) {
  const { slug } = await params;
  const template = findTemplate(slug);
  if (!template) {
    notFound();
  }
  const related: readonly TemplateSummary[] = findRelated(template).map(
    ({ slug: relatedSlug, title, tagline, topology, useCases, language, clients, products, stack, agentReady }) => ({
      slug: relatedSlug,
      title,
      tagline,
      topology,
      useCases,
      language,
      clients,
      products,
      stack,
      agentReady,
    }),
  );
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
