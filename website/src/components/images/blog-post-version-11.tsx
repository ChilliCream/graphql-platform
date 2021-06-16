import { graphql, useStaticQuery } from "gatsby";
import { GatsbyImage } from "gatsby-plugin-image";
import React, { FunctionComponent } from "react";
import styled from "styled-components";
import { GetBlogPostVersion11ImageQuery } from "../../../graphql-types";

export const BlogPostVersion11: FunctionComponent = () => {
  const data = useStaticQuery<GetBlogPostVersion11ImageQuery>(graphql`
    query getBlogPostVersion11Image {
      file(
        relativePath: {
          eq: "2020-11-23-hot-chocolate-11/hot-chocolate-11-banner.png"
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
        alt="Welcome Hot Chocolate 11"
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
