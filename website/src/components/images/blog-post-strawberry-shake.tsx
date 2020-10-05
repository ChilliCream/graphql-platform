import { graphql, useStaticQuery } from "gatsby";
import Img from "gatsby-image";
import React, { FunctionComponent } from "react";
import styled from "styled-components";
import { GetBlogPostStrawberryShakeImageQuery } from "../../../graphql-types";

export const BlogPostStrawberryShake: FunctionComponent = () => {
  const data = useStaticQuery<GetBlogPostStrawberryShakeImageQuery>(graphql`
    query getBlogPostStrawberryShakeImage {
      placeholderImage: file(
        relativePath: { eq: "shared/strawberry-shake-banner.png" }
        sourceInstanceName: { eq: "blog" }
      ) {
        childImageSharp {
          fluid(maxWidth: 1200) {
            ...GatsbyImageSharpFluid
          }
        }
      }
    }
  `);

  return (
    <Container>
      <Img fluid={data.placeholderImage?.childImageSharp?.fluid as any} />
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
