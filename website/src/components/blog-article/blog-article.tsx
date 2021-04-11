import { graphql } from "gatsby";
import Img, { FluidObject } from "gatsby-image";
import { MDXRenderer } from "gatsby-plugin-mdx";
import React, { FunctionComponent } from "react";
import styled from "styled-components";
import { BlogArticleFragment } from "../../../graphql-types";
import { ArticleComments } from "../articles/article-comments";
import {
  ArticleContent,
  ArticleHeader,
  ArticleTitle,
} from "../articles/article-elements";
import { BlogArticleMetadata } from "./blog-article-metadata";
import { BlogArticleSharebar } from "./blog-article-sharebar";
import { BlogArticleTags } from "./blog-article-tags";
import { Article } from "../articles/article";

interface BlogArticleProperties {
  data: BlogArticleFragment;
}

export const BlogArticle: FunctionComponent<BlogArticleProperties> = ({
  data,
}) => {
  const { mdx } = data;
  const { frontmatter, body } = mdx!;
  const path = frontmatter!.path!;
  const title = frontmatter!.title!;
  const existingTags: string[] = frontmatter!.tags!
    ? (frontmatter!.tags!.filter((tag) => tag && tag.length > 0) as string[])
    : [];
  const featuredImage = frontmatter!.featuredImage?.childImageSharp
    ?.fluid as FluidObject;

  return (
    <Container>
      <BlogArticleSharebar data={data} tags={existingTags} />
      <ArticleWrapper>
        <Article>
          <ArticleHeader>
            {featuredImage && <Img fluid={featuredImage} />}
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
            fluid(maxWidth: 800, pngQuality: 90) {
              ...GatsbyImageSharpFluid
            }
          }
        }
        path
        title
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

  @media only screen and (min-width: 820px) {
    padding: 20px 10px 0;
  }
`;

const Container = styled.div`
  display: flex;
  flex: 0 0 auto;
  flex-direction: row;
  width: 100%;
  max-width: 820px;
`;
