import { graphql, useStaticQuery } from "gatsby";
import Img from "gatsby-image";
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
          fluid(maxWidth: 1200, pngQuality: 90) {
            ...GatsbyImageSharpFluid
          }
        }
      }
    }
  `);

  return (
    <Container>
      <Img fluid={data.file?.childImageSharp?.fluid as any} />
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
