import { notFound } from "next/navigation";
import React from "react";

import IntegrationDetailPage from "@/page-components/integration-detail";
import {
  findIntegration,
  INTEGRATIONS,
} from "@/data/integrations/integrations";

interface PageProps {
  readonly params: Promise<{ readonly slug: string }>;
}

export function generateStaticParams(): { slug: string }[] {
  return INTEGRATIONS.map((i) => ({ slug: i.slug }));
}

export default async function Page({ params }: PageProps) {
  const { slug } = await params;
  const integration = findIntegration(slug);
  if (!integration) {
    notFound();
  }
  return <IntegrationDetailPage integration={integration} />;
}
