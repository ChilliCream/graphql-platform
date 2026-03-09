"use client";

import React, { FC } from "react";

import { ContentSection } from "@/components/misc";
import { SrOnly } from "@/components/misc/sr-only";
import { RecentBlogPost } from "./most-recent-blog-posts-section";
import { BlogArticleTeaser } from "./blog-article-teaser";
import { Boxes } from "./box-elements";

export interface MostRecentNitroBlogPostsSectionProps {
  posts?: RecentBlogPost[];
}

export const MostRecentNitroBlogPostsSection: FC<
  MostRecentNitroBlogPostsSectionProps
> = ({ posts = [] }) => {
  return (
    <ContentSection title="From Our Blog" noBackground>
      <SrOnly>
        Here you find the latest news about Nitro the GraphQL IDE to explore and
        test any GraphQL API.
      </SrOnly>
      <Boxes>
        {posts.map((node) => (
          <BlogArticleTeaser key={`article-${node.id}`} data={node} />
        ))}
      </Boxes>
    </ContentSection>
  );
};
