"use client";

import React, { FC } from "react";

import { ContentSection, SrOnly } from "@/components/misc";
import { BlogArticleTeaser } from "./blog-article-teaser";
import { Boxes } from "./box-elements";

export interface RecentBlogPost {
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

export interface MostRecentBlogPostsSectionProps {
  posts?: RecentBlogPost[];
}

export const MostRecentBlogPostsSection: FC<
  MostRecentBlogPostsSectionProps
> = ({ posts = [] }) => {
  return (
    <ContentSection title="From Our Blog" noBackground>
      <SrOnly>
        Here you find the latest news about the ChilliCream and its entire
        GraphQL Platform.
      </SrOnly>
      <Boxes>
        {posts.map((node) => (
          <BlogArticleTeaser key={`article-${node.id}`} data={node} />
        ))}
      </Boxes>
    </ContentSection>
  );
};
