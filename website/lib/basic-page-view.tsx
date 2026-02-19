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
import { DefaultArticleNavigation } from "@/components/articles/default-article-navigation";
import { ResponsiveArticleMenu } from "@/components/articles/responsive-article-menu";
import { MdxContent } from "./mdx-content";

interface NavLink {
  path: string;
  title: string;
}

interface BasicPageViewProps {
  title: string;
  slug: string;
  mdxSource: MDXRemoteSerializeResult;
  headings: Array<{ depth: number; value: string }>;
  navigationLinks: NavLink[];
}

export function BasicPageView({
  title,
  slug,
  mdxSource,
  headings,
  navigationLinks,
}: BasicPageViewProps) {
  const navigationData = {
    navigation: {
      links: navigationLinks,
    },
  };

  const tocData = {
    headings,
  };

  return (
    <SiteLayout>
      <SEO title={title} />
      <ArticleLayout
        navigation={
          <DefaultArticleNavigation
            data={navigationData}
            selectedPath={slug}
          />
        }
        aside={<ArticleTableOfContent data={tocData} />}
      >
        <ArticleHeader>
          <ResponsiveArticleMenu />
          <ArticleTitle>{title}</ArticleTitle>
        </ArticleHeader>
        <ArticleContent>
          <MdxContent source={mdxSource} />
        </ArticleContent>
      </ArticleLayout>
    </SiteLayout>
  );
}
