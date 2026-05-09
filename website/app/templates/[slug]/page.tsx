import { notFound } from "next/navigation";
import React from "react";

import TemplateDetailPage from "@/page-components/template-detail";
import { findTemplate, TEMPLATES } from "@/data/templates/templates";

interface PageProps {
  readonly params: Promise<{ readonly slug: string }>;
}

export function generateStaticParams(): { slug: string }[] {
  return TEMPLATES.map((t) => ({ slug: t.slug }));
}

export default async function Page({ params }: PageProps) {
  const { slug } = await params;
  const template = findTemplate(slug);
  if (!template) {
    notFound();
  }
  return <TemplateDetailPage template={template} />;
}
