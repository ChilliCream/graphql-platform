import { notFound } from "next/navigation";
import React from "react";

import CustomerStoryPage from "@/page-components/customer-story";
import { findStory, STORIES } from "@/data/customers/stories";

interface PageProps {
  readonly params: Promise<{ readonly slug: string }>;
}

export function generateStaticParams(): { slug: string }[] {
  return STORIES.map((s) => ({ slug: s.slug }));
}

export default async function Page({ params }: PageProps) {
  const { slug } = await params;
  const story = findStory(slug);
  if (!story) {
    notFound();
  }
  return <CustomerStoryPage story={story} />;
}
