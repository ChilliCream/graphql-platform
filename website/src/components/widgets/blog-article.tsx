import { graphql } from "gatsby";
import Img, { FluidObject } from "gatsby-image";
import React, { FunctionComponent } from "react";
import styled from "styled-components";
import { BlogArticleFragment } from "../../../graphql-types";
import { ArticleComments } from "../misc/article-comments";
import {
  Article,
  ArticleContent,
  ArticleHeader,
  ArticleTitle,
  ArticleWrapper,
} from "../misc/article-elements";
import { BlogArticleMetadata } from "../misc/blog-article-metadata";
import { BlogArticleSharebar } from "../misc/blog-article-sharebar";
import { BlogArticleTags } from "../misc/blog-article-tags";

interface BlogArticleProperties {
  data: BlogArticleFragment;
}

export const BlogArticle: FunctionComponent<BlogArticleProperties> = ({
  data,
}) => {
  const { markdownRemark } = data;
  const { frontmatter, html } = markdownRemark!;
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
            <BlogArticleMetadata data={markdownRemark!} />
            <BlogArticleTags tags={existingTags} />
          </ArticleHeader>
          <ArticleContent dangerouslySetInnerHTML={{ __html: html! }} />
        </Article>
        <ArticleComments data={data} path={path} title={title} />
      </ArticleWrapper>
    </Container>
  );
};

export const BlogArticleGraphQLFragment = graphql`
  fragment BlogArticle on Query {
    markdownRemark(frontmatter: { path: { eq: $path } }) {
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
      html
      ...BlogArticleMetadata
    }
    ...ArticleComments
    ...BlogArticleSharebar
  }
`;

const Container = styled.div`
  display: flex;
  flex: 0 0 auto;
  flex-direction: row;
  width: 100%;
  max-width: 820px;
`;
