import { graphql, useStaticQuery } from "gatsby";
import Img from "gatsby-image";
import React, { FunctionComponent } from "react";
import styled from "styled-components";
import { GetEfMeetsGraphQlImageQuery } from "../../../graphql-types";

export const EFMeetsGraphQL: FunctionComponent = () => {
  const data = useStaticQuery<GetEfMeetsGraphQlImageQuery>(graphql`
    query getEFMeetsGraphQLImage {
      placeholderImage: file(
        relativePath: { eq: "ef-meets-graphql.png" }
        sourceInstanceName: { eq: "images" }
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
