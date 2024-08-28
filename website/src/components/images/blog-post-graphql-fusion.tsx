import { graphql, useStaticQuery } from "gatsby";
import { GatsbyImage } from "gatsby-plugin-image";
import React, { FC } from "react";
import styled from "styled-components";

import { GetBlogPostHotChocolate14ImageQuery } from "@/graphql-types";

export const BlogPostHotChocolate14: FC = () => {
  const data = useStaticQuery<GetBlogPostHotChocolate14ImageQuery>(graphql`
    query getBlogPostHotChocolate14Image {
      file(
        relativePath: { eq: "2024-08-30-hot-chocolate-14/hot-chocolate-14.png" }
        sourceInstanceName: { eq: "blog" }
      ) {
        childImageSharp {
          gatsbyImageData(layout: CONSTRAINED, width: 1200, quality: 100)
        }
      }
    }
  `);

  return (
    <Container>
      <GatsbyImage
        image={data.file?.childImageSharp?.gatsbyImageData}
        alt="Hot Chocolate Version 14"
      />
    </Container>
  );
};

const Container = styled.div`
  padding: 30px;

  .gatsby-image-wrapper {
    border-radius: var(--border-radius);
    box-shadow: 0 9px 18px rgba(0, 0, 0, 0.25);
  }
`;
