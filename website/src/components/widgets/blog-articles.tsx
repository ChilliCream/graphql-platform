import { graphql } from "gatsby";
import Img, { FluidObject } from "gatsby-image";
import React, { FunctionComponent } from "react";
import styled from "styled-components";
import { BlogArticlesFragment } from "../../../graphql-types";
import { ArticleTitle } from "../misc/article-elements";
import { BlogArticleMetadata } from "../misc/blog-article-metadata";
import { BlogArticleTags } from "../misc/blog-article-tags";
import { Link } from "../misc/link";
import { Pagination } from "../misc/pagination";

interface BlogArticlesProperties {
  currentPage?: number;
  data: BlogArticlesFragment;
  totalPages?: number;
}

export const BlogArticles: FunctionComponent<BlogArticlesProperties> = ({
  currentPage,
  data: { edges },
  totalPages,
}) => {
  return (
    <>
      <Container>
        {edges.map(({ node }) => {
          const existingTags: string[] = node?.frontmatter?.tags
            ? (node.frontmatter.tags.filter(
                (tag) => tag && tag.length > 0
              ) as string[])
            : [];
          const featuredImage = node?.frontmatter!.featuredImage
            ?.childImageSharp?.fluid as FluidObject;

          return (
            <Article key={`article-${node.id}`}>
              <Link to={node.frontmatter!.path!}>
                {featuredImage && <Img fluid={featuredImage} />}
                <ArticleTitle>{node.frontmatter!.title}</ArticleTitle>
              </Link>
              <BlogArticleMetadata data={node!} />
              <BlogArticleTags tags={existingTags} />
            </Article>
          );
        })}
      </Container>
      {currentPage && totalPages && (
        <Pagination
          currentPage={currentPage}
          linkPrefix="/blog"
          totalPages={totalPages}
        />
      )}
    </>
  );
};

export const BlogArticlesGraphQLFragment = graphql`
  fragment BlogArticles on MarkdownRemarkConnection {
    edges {
      node {
        id
        frontmatter {
          featuredImage {
            childImageSharp {
              fluid(maxWidth: 800) {
                ...GatsbyImageSharpFluid
              }
            }
          }
          path
          title
          ...BlogArticleTags
        }
        ...BlogArticleMetadata
      }
    }
  }
`;

const Container = styled.ul`
  display: flex;
  flex: 0 0 auto;
  flex-direction: column;
  margin: 0;
  width: 100%;
  max-width: 800px;
  list-style-type: none;
`;

const Article = styled.li`
  margin-bottom: 15px;

  @media only screen and (min-width: 800px) {
    border: 1px solid #ccc;
  }
`;
