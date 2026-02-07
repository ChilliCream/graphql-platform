"use client";

import React from "react";
import { MDXRemoteSerializeResult } from "next-mdx-remote";

import {
  ArticleContent,
  ArticleHeader,
  ArticleTitle,
} from "@/components/article-elements";
import { ArticleLayout, SiteLayout } from "@/components/layout";
import { SEO } from "@/components/misc";
import { ArticleTableOfContent } from "@/components/articles/article-table-of-content";
import { DocArticleCommunity } from "@/components/articles/doc-article-community";
import { DocArticleNavigation } from "@/components/articles/doc-article-navigation";
import { ResponsiveArticleMenu } from "@/components/articles/responsive-article-menu";
import { MdxContent } from "./mdx-content";
import type { DocPage, DocsProduct } from "./docs";

interface DocPageViewProps {
  page: DocPage;
  mdxSource: MDXRemoteSerializeResult;
  docsConfig: DocsProduct[];
  slug: string[];
  headings: Array<{ depth: number; value: string }>;
}

export function DocPageView({
  page,
  mdxSource,
  docsConfig,
  slug,
  headings,
}: DocPageViewProps) {
  const productPath = slug[0];
  const versionPath =
    slug.length > 1 && /^v\d+/.test(slug[1]) ? slug[1] : "";
  const selectedPath = "/docs/" + slug.join("/");
  const title = page.frontmatter?.title || slug[slug.length - 1];
  const description = page.frontmatter?.description;

  const navData = { config: { products: docsConfig } };

  return (
    <SiteLayout>
      <SEO title={title} description={page.frontmatter?.description} />
      <ArticleLayout
        navigation={
          <DocArticleNavigation
            data={navData}
            selectedPath={selectedPath}
            selectedProduct={productPath}
            selectedVersion={versionPath}
          />
        }
        aside={
          <>
            <DocArticleCommunity originPath={page.originPath} />
            <ArticleTableOfContent data={{ headings }} />
          </>
        }
      >
        <ArticleHeader>
          <ResponsiveArticleMenu />
          <ArticleTitle>{title}</ArticleTitle>
        </ArticleHeader>
        <ArticleContent>
          {description && <p>{description}</p>}
          <MdxContent source={mdxSource} />
        </ArticleContent>
      </ArticleLayout>
    </SiteLayout>
  );
}
