import React from "react";
import fs from "fs";
import path from "path";

import { compileMdxContent, extractHeadings } from "@/lib/mdx";
import { createMetadata } from "@/lib/metadata";
import { BasicPageView } from "@/lib/basic-page-view";
import {
  readMarkdownFile,
  getContentDir,
  getBasicPageNavLinks,
} from "@/lib/content";
import { notFound } from "next/navigation";

interface PageProps {
  params: Promise<{ slug: string }>;
}

const LICENSING_DIR = getContentDir("basic", "licensing");

export async function generateStaticParams() {
  if (!fs.existsSync(LICENSING_DIR)) return [];

  const files = fs.readdirSync(LICENSING_DIR).filter((f) => f.endsWith(".md"));

  return files.map((f) => ({
    slug: f.replace(/\.md$/, ""),
  }));
}

export async function generateMetadata({ params }: PageProps) {
  const { slug } = await params;
  const filePath = path.join(LICENSING_DIR, `${slug}.md`);

  if (!fs.existsSync(filePath)) {
    // TODO: What about this
    return createMetadata({ title: "Not Found" });
  }

  const { frontmatter } = readMarkdownFile(filePath);
  return createMetadata({ title: frontmatter.title || slug });
}

export default async function LicensingPage({ params }: PageProps) {
  const { slug } = await params;
  const filePath = path.join(LICENSING_DIR, `${slug}.md`);

  if (!fs.existsSync(filePath)) {
    return notFound();
  }

  const { frontmatter, content: rawContent } = readMarkdownFile(filePath);
  const { mdxSource } = await compileMdxContent(rawContent);
  const headings = extractHeadings(rawContent);
  const navigationLinks = getBasicPageNavLinks();

  return (
    <BasicPageView
      title={frontmatter.title || slug}
      slug={`/licensing/${slug}`}
      mdxSource={mdxSource}
      headings={headings}
      navigationLinks={navigationLinks}
    />
  );
}
