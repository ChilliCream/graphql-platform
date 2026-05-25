import React, { FC } from "react";

import { ContentSection, Pagination } from "@/components/misc";
import { BlogArticleTeaser } from "./blog-article-teaser";
import { Boxes } from "./box-elements";

interface BlogPostNode {
  id: string;
  frontmatter?: {
    featuredImage?: string;
    path?: string;
    title?: string;
    author?: string;
    authorImageUrl?: string;
    date?: string;
  };
  fields?: {
    readingTime?: {
      text?: string;
    };
  };
}

interface AllBlogPostsData {
  edges: Array<{
    node: BlogPostNode;
  }>;
}

export interface AllBlogPostsProps {
  readonly data: AllBlogPostsData;
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
