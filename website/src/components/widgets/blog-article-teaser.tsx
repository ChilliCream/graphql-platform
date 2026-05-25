import React, { FC } from "react";

import { BlogArticleTeaserMetadata } from "./blog-article-teaser-metadata";
import { Box, BoxLink } from "./box-elements";

interface BlogArticleTeaserData {
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

export interface BlogArticleTeaserProps {
  readonly data: BlogArticleTeaserData;
}

export const BlogArticleTeaser: FC<BlogArticleTeaserProps> = ({ data }) => {
  const featuredImage = data.frontmatter?.featuredImage;

  return (
    <Box key={`article-${data.id}`}>
      <BoxLink to={data.frontmatter?.path || "#"}>
        {featuredImage && (
          <img
            src={featuredImage}
            alt={data.frontmatter?.title || ""}
            width={800}
            height={450}
            loading="lazy"
            decoding="async"
            style={{ width: "100%", height: "auto" }}
          />
        )}
        <BlogArticleTeaserMetadata data={data} />
      </BoxLink>
    </Box>
  );
};
