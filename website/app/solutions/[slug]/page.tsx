import { notFound } from "next/navigation";
import React from "react";

import SolutionPage from "@/page-components/solution";
import { findSolution, SOLUTIONS } from "@/data/solutions/solutions";

interface PageProps {
  readonly params: Promise<{ readonly slug: string }>;
}

export function generateStaticParams(): { slug: string }[] {
  return SOLUTIONS.map((s) => ({ slug: s.slug }));
}

export default async function Page({ params }: PageProps) {
  const { slug } = await params;
  const record = findSolution(slug);
  if (!record) {
    notFound();
  }
  return <SolutionPage record={record} />;
}
