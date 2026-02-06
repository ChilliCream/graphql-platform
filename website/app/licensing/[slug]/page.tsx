import React from "react";
import fs from "fs";
import path from "path";

import { compileMdxContent } from "@/lib/mdx";
import { createMetadata } from "@/lib/metadata";
import { BasicPageView } from "@/lib/basic-page-view";
import { readMarkdownFile, getContentDir } from "@/lib/content";

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
    return createMetadata({ title: "Not Found" });
  }

  const { frontmatter } = readMarkdownFile(filePath);
  return createMetadata({ title: frontmatter.title || slug });
}

export default async function LicensingPage({ params }: PageProps) {
  const { slug } = await params;
  const filePath = path.join(LICENSING_DIR, `${slug}.md`);

  if (!fs.existsSync(filePath)) {
    return <div>Not found</div>;
  }

  const { frontmatter, content: rawContent } = readMarkdownFile(filePath);
  const { mdxSource } = await compileMdxContent(rawContent);

  return (
    <BasicPageView
      title={frontmatter.title || slug}
      mdxSource={mdxSource}
    />
  );
}
