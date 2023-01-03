import { graphql } from "gatsby";
import { GatsbyImage } from "gatsby-plugin-image";
import { MDXRenderer } from "gatsby-plugin-mdx";
import React, { FC } from "react";
import styled from "styled-components";

import { Article } from "@/components/articles/article";
import { ArticleComments } from "@/components/articles/article-comments";
import {
  ArticleContent,
  ArticleHeader,
  ArticleHeaderVideoContainer,
  ArticleTitle,
  ArticleVideo,
} from "@/components/articles/article-elements";
import { BlogArticleFragment } from "@/graphql-types";
import { BlogArticleMetadata } from "./blog-article-metadata";
import { BlogArticleSharebar } from "./blog-article-sharebar";
import { BlogArticleTags } from "./blog-article-tags";

export interface BlogArticleProps {
  readonly data: BlogArticleFragment;
}

export const BlogArticle: FC<BlogArticleProps> = ({ data }) => {
  const { mdx } = data;
  const { frontmatter, body } = mdx!;
  const path = frontmatter!.path!;
  const title = frontmatter!.title!;
  const existingTags: string[] = frontmatter!.tags!
    ? (frontmatter!.tags!.filter((tag) => tag && tag.length > 0) as string[])
    : [];
  const featuredImage =
    frontmatter!.featuredImage?.childImageSharp?.gatsbyImageData;
  const featuredVideoId = frontmatter!.featuredVideoId;

  return (
    <Container>
      <BlogArticleSharebar data={data} tags={existingTags} />
      <ArticleWrapper>
        <Article>
          <ArticleHeader kind="blog">
            {featuredVideoId && (
              <ArticleHeaderVideoContainer>
                <ArticleVideo videoId={featuredVideoId} />
              </ArticleHeaderVideoContainer>
            )}
            {featuredImage && !featuredVideoId && (
              <GatsbyImage image={featuredImage} alt={title} />
            )}
            <ArticleTitle>{title}</ArticleTitle>
            <BlogArticleMetadata data={mdx!} />
            <BlogArticleTags tags={existingTags} />
          </ArticleHeader>
          <ArticleContent>
            <MDXRenderer>{body}</MDXRenderer>
          </ArticleContent>
        </Article>
        <ArticleComments data={data} path={path} title={title} />
      </ArticleWrapper>
    </Container>
  );
};

export const BlogArticleGraphQLFragment = graphql`
  fragment BlogArticle on Query {
    mdx(frontmatter: { path: { eq: $path } }) {
      excerpt
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
      ...BlogArticleMetadata
    }
    ...ArticleComments
    ...BlogArticleSharebar
  }
`;

const ArticleWrapper = styled.div`
  display: grid;
  grid-template-rows: 1fr auto;
  padding: 0;

  @media only screen and (min-width: 860px) {
    padding: 20px 10px 0;
  }
`;

const Container = styled.div`
  display: flex;
  flex: 0 0 auto;
  flex-direction: row;
  width: 100%;

  @media only screen and (min-width: 860px) {
    max-width: 820px;
  }
`;
