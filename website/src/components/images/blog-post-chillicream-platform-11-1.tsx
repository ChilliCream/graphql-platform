import { graphql, useStaticQuery } from "gatsby";
import { GatsbyImage } from "gatsby-plugin-image";
import React, { FunctionComponent } from "react";
import styled from "styled-components";
import { GetBlogPostChilliCreamPlatformImageQuery } from "../../../graphql-types";

export const BlogPostChilliCreamPlatform: FunctionComponent = () => {
  const data = useStaticQuery<GetBlogPostChilliCreamPlatformImageQuery>(graphql`
    query getBlogPostChilliCreamPlatformImage {
      file(
        relativePath: {
          eq: "2021-03-31-chillicream-platform-11-1/chillicream-platform-11-1-banner.png"
        }
        sourceInstanceName: { eq: "blog" }
      ) {
        childImageSharp {
          gatsbyImageData(
            layout: CONSTRAINED
            width: 1200
            pngOptions: { quality: 90 }
          )
        }
      }
    }
  `);

  return (
    <Container>
      <GatsbyImage
        image={data.file?.childImageSharp?.gatsbyImageData}
        alt="ChilliCream Platform Update 11.1"
      />
    </Container>
  );
};

const Container = styled.div`
  padding: 30px;

  .gatsby-image-wrapper {
    border-radius: 4px;
    box-shadow: 0 9px 18px rgba(0, 0, 0, 0.25);
  }
`;
