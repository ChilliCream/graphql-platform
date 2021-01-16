import { graphql, useStaticQuery } from "gatsby";
import Img from "gatsby-image";
import React, { FunctionComponent } from "react";
import styled from "styled-components";
import { GetBlogPostEfMeetsGraphQlImageQuery } from "../../../graphql-types";

export const BlogPostEFMeetsGraphQL: FunctionComponent = () => {
  const data = useStaticQuery<GetBlogPostEfMeetsGraphQlImageQuery>(graphql`
    query getBlogPostEFMeetsGraphQLImage {
      file(
        relativePath: {
          eq: "2020-03-18-entity-framework/banner-entityframework.png"
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
