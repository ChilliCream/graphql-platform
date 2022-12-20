import { graphql } from "gatsby";
import { GatsbyImage } from "gatsby-plugin-image";
import React, { FC } from "react";
import styled from "styled-components";

import { ArticleTitle } from "@/components/articles/article-elements";
import { BlogArticleMetadata } from "@/components/blog-article/blog-article-metadata";
import { BlogArticleTags } from "@/components/blog-article/blog-article-tags";
import { Link } from "@/components/misc/link";
import { Pagination } from "@/components/misc/pagination";
import { BlogArticlesFragment } from "@/graphql-types";

export interface BlogArticlesProps {
  readonly currentPage?: number;
  readonly data: BlogArticlesFragment;
  readonly totalPages?: number;
}

export const BlogArticles: FC<BlogArticlesProps> = ({
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
          const featuredImage =
            node?.frontmatter!.featuredImage?.childImageSharp?.gatsbyImageData;

          return (
            <Article key={`article-${node.id}`}>
              <Link to={node.frontmatter!.path!}>
                {featuredImage && (
                  <GatsbyImage
                    image={featuredImage}
                    alt={node.frontmatter!.title}
                  />
                )}
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
  fragment BlogArticles on MdxConnection {
    edges {
      node {
        id
        frontmatter {
          featuredImage {
            childImageSharp {
              gatsbyImageData(layout: CONSTRAINED, width: 800, quality: 100)
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
  margin: 0 0 60px;
  width: 100%;
  max-width: 800px;
  list-style-type: none;
`;

const Article = styled.li`
  @media only screen and (min-width: 860px) {
    margin: 20px 0 0;
    border-radius: var(--border-radius);
    box-shadow: 0 3px 6px rgba(0, 0, 0, 0.25);

    > a > .gatsby-image-wrapper {
      border-radius: var(--border-radius) var(--border-radius) 0 0;
    }
  }
`;
