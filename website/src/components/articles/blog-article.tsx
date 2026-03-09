import React, { FC, ReactNode } from "react";
import styled from "styled-components";

import LiteYouTubeEmbed from "react-lite-youtube-embed";
import "react-lite-youtube-embed/dist/LiteYouTubeEmbed.css";

import {
  ArticleContent,
  ArticleHeader,
  ArticleHeaderVideoContainer,
  ArticleTitle,
} from "@/components/article-elements";
import { ArticleLayout } from "@/components/layout";
import { ArticleTableOfContent } from "./article-table-of-content";
import { BlogArticleMetadata } from "./blog-article-metadata";
import { BlogArticleNavigation } from "./blog-article-navigation";
import { BlogArticleSharebar } from "./blog-article-sharebar";
import { BlogArticleTags } from "./blog-article-tags";
import { ResponsiveArticleMenu } from "./responsive-article-menu";

interface BlogArticleMdx {
  excerpt?: string;
  fields?: {
    slug?: string;
    readingTime?: {
      text?: string;
    };
  };
  frontmatter?: {
    featuredImage?: string;
    featuredVideoId?: string;
    path?: string;
    title?: string;
    description?: string;
    tags?: Array<string | null>;
    author?: string;
    authorImageUrl?: string;
    date?: string;
  };
  headings?: Array<{
    depth?: number;
    value?: string;
  } | null>;
}

interface BlogArticleData {
  mdx?: BlogArticleMdx;
}

export interface BlogArticleProps {
  readonly data: BlogArticleData;
  readonly content: ReactNode;
}

export const BlogArticle: FC<BlogArticleProps> = ({ data, content }) => {
  const { mdx } = data;
  const { fields, frontmatter } = mdx || {};
  const title = frontmatter?.title || "";
  const existingTags: string[] = frontmatter?.tags
    ? (frontmatter.tags.filter((tag) => tag && tag.length > 0) as string[])
    : [];
  const featuredImage = frontmatter?.featuredImage;
  const featuredVideoId = frontmatter?.featuredVideoId;

  return (
    <ArticleLayout
      navigation={
        <BlogArticleNavigation data={data} selectedPath={fields?.slug ?? ""} />
      }
      aside={<ArticleTableOfContent data={mdx || {}} />}
    >
      <ArticleHeader>
        <ResponsiveArticleMenu />
        {featuredVideoId && (
          <ArticleHeaderVideoContainer>
            <LiteYouTubeEmbed id={featuredVideoId} title={title} />
          </ArticleHeaderVideoContainer>
        )}
        {featuredImage && !featuredVideoId && (
          <img
            src={featuredImage}
            alt={title}
            width={1200}
            height={675}
            loading="lazy"
            decoding="async"
            style={{ width: "100%", height: "auto" }}
          />
        )}
        <ArticleTitle>{title}</ArticleTitle>
        <Metadata>
          <BlogArticleMetadata data={mdx || {}} />
          <BlogArticleSharebar data={data} tags={existingTags} />
        </Metadata>
        <BlogArticleTags tags={existingTags} />
      </ArticleHeader>
      <ArticleContent>{content}</ArticleContent>
    </ArticleLayout>
  );
};

const Metadata = styled.div`
  display: flex;
  flex-direction: row;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 24px;
`;
