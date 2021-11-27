import { graphql, useStaticQuery } from "gatsby";
import { GatsbyImage } from "gatsby-plugin-image";
import React, { FC } from "react";
import styled from "styled-components";
import { GetBlogPostBananaCakePopCloudImageQuery } from "../../../graphql-types";

export const BlogPostBananaCakePopCloud: FC = () => {
  const data = useStaticQuery<GetBlogPostBananaCakePopCloudImageQuery>(graphql`
    query getBlogPostBananaCakePopCloudImage {
      file(
        relativePath: {
          eq: "2021-11-22-banana-cake-pop-cloud/banana-cake-pop-cloud-banner.png"
        }
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
        alt="Hot Chocolate Version 12"
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
