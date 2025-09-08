import { graphql } from "gatsby";
import React, { FC } from "react";

import { ContentSection, Pagination } from "@/components/misc";
import { AllBlogPostsFragment } from "@/graphql-types";
import { BlogArticleTeaser } from "./blog-article-teaser";
import { Boxes } from "./box-elements";

export interface AllBlogPostsProps {
  readonly data: AllBlogPostsFragment;
  readonly description: string;
  readonly currentPage: number;
  readonly totalPages: number;
  readonly basePath: string;
}

export const AllBlogPosts: FC<AllBlogPostsProps> = ({
  data: { edges },
  description,
  currentPage,
  totalPages,
  basePath,
}) => {
  return (
    <>
      <ContentSection title="Blog" text={description} noBackground>
        <Boxes>
          {edges.map(({ node }) => (
            <BlogArticleTeaser key={`article-${node.id}`} data={node} />
          ))}
        </Boxes>
      </ContentSection>
      <Pagination
        currentPage={currentPage}
        linkPrefix={basePath}
        totalPages={totalPages}
      />
    </>
  );
};

export const BlogArticlesGraphQLFragment = graphql`
  fragment AllBlogPosts on MdxConnection {
    edges {
      node {
        id
        ...BlogArticleTeaser
      }
    }
  }
`;
