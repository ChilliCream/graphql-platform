import { graphql, useStaticQuery } from "gatsby";
import { GatsbyImage } from "gatsby-plugin-image";
import React, { FC } from "react";
import styled from "styled-components";

import { GetFullStackWorkshopQueryImage } from "@/graphql-types";

export const FullstackWorkshopImage: FC = () => {
  const data = useStaticQuery<GetFullStackWorkshopQueryImage>(graphql`
    query getFullStackWorkshopImage {
      file(
        relativePath: { eq: "2024-04-01-fullstack-workshop/header.png" }
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
        alt="Fullstack Workshop"
      />
    </Container>
  );
};

const Container = styled.div`
  padding: 30px;

  .gatsby-image-wrapper {
    border-radius: var(--border-radius);
  }
`;
