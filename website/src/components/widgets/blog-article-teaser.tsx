import { graphql } from "gatsby";
import React, { FC } from "react";

import { BlogArticleTeaserFragment } from "@/graphql-types";
import { GatsbyImage } from "gatsby-plugin-image";
import { BlogArticleTeaserMetadata } from "./blog-article-teaser-metadata";
import { Box, BoxLink } from "./box-elements";

export interface BlogArticleTeaserProps {
  readonly data: BlogArticleTeaserFragment;
}

export const BlogArticleTeaser: FC<BlogArticleTeaserProps> = ({ data }) => {
  const featuredImage =
    data.frontmatter!.featuredImage?.childImageSharp?.gatsbyImageData;

  return (
    <Box key={`article-${data.id}`}>
      <BoxLink to={data.frontmatter!.path!}>
        {featuredImage && (
          <GatsbyImage image={featuredImage} alt={data.frontmatter!.title} />
        )}
        <BlogArticleTeaserMetadata data={data!} />
      </BoxLink>
    </Box>
  );
};

export const BlogArticleTeaserGraphQLFragment = graphql`
  fragment BlogArticleTeaser on Mdx {
    id
    frontmatter {
      featuredImage {
        childImageSharp {
          gatsbyImageData(layout: CONSTRAINED, width: 800, quality: 100)
        }
      }
      path
      title
    }
    ...BlogArticleTeaserMetadata
  }
`;
