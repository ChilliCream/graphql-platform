import { graphql } from "gatsby";
import { GatsbyImage } from "gatsby-plugin-image";
import { MDXRenderer } from "gatsby-plugin-mdx";
import React, { FC } from "react";
import styled from "styled-components";

import {
  ArticleContent,
  ArticleHeader,
  ArticleHeaderVideoContainer,
  ArticleTitle,
  ArticleVideo,
} from "@/components/article-elements";
import { ArticleLayout } from "@/components/layout";
import { BlogArticleFragment } from "@/graphql-types";
import { ArticleTableOfContent } from "./article-table-of-content";
import { BlogArticleMetadata } from "./blog-article-metadata";
import { BlogArticleNavigation } from "./blog-article-navigation";
import { BlogArticleSharebar } from "./blog-article-sharebar";
import { BlogArticleTags } from "./blog-article-tags";
import { ResponsiveArticleMenu } from "./responsive-article-menu";

export interface BlogArticleProps {
  readonly data: BlogArticleFragment;
}

export const BlogArticle: FC<BlogArticleProps> = ({ data }) => {
  const { mdx } = data;
  const { fields, frontmatter, body } = mdx!;
  const title = frontmatter!.title!;
  const existingTags: string[] = frontmatter!.tags!
    ? (frontmatter!.tags!.filter((tag) => tag && tag.length > 0) as string[])
    : [];
  const featuredImage =
    frontmatter!.featuredImage?.childImageSharp?.gatsbyImageData;
  const featuredVideoId = frontmatter!.featuredVideoId;

  return (
    <ArticleLayout
      navigation={
        <BlogArticleNavigation data={data} selectedPath={fields?.slug ?? ""} />
      }
      aside={<ArticleTableOfContent data={data.mdx!} />}
    >
      <ArticleHeader>
        <ResponsiveArticleMenu />
        {featuredVideoId && (
          <ArticleHeaderVideoContainer>
            <ArticleVideo videoId={featuredVideoId} />
          </ArticleHeaderVideoContainer>
        )}
        {featuredImage && !featuredVideoId && (
          <GatsbyImage image={featuredImage} alt={title} />
        )}
        <ArticleTitle>{title}</ArticleTitle>
        <Metadata>
          <BlogArticleMetadata data={mdx!} />
          <BlogArticleSharebar data={data} tags={existingTags} />
        </Metadata>
        <BlogArticleTags tags={existingTags} />
      </ArticleHeader>
      <ArticleContent>
        <MDXRenderer>{body}</MDXRenderer>
      </ArticleContent>
    </ArticleLayout>
  );
};

export const BlogArticleGraphQLFragment = graphql`
  fragment BlogArticle on Query {
    mdx(frontmatter: { path: { eq: $path } }) {
      excerpt
      fields {
        slug
      }
      frontmatter {
        featuredImage {
          childImageSharp {
            gatsbyImageData(layout: CONSTRAINED, width: 800, quality: 100)
          }
        }
        featuredVideoId
        path
        title
        description
        ...BlogArticleTags
      }
      body
      ...ArticleSections
      ...BlogArticleMetadata
    }
    ...BlogArticleNavigation
    ...BlogArticleSharebar
  }
`;

const Metadata = styled.div`
  display: flex;
  flex-direction: row;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 24px;
`;
