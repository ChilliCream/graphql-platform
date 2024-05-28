import { graphql, useStaticQuery } from "gatsby";
import { GatsbyImage } from "gatsby-plugin-image";
import React, { FC } from "react";
import styled from "styled-components";

import { GetFullStackWorkshopQuery } from "@/graphql-types";

export const FullstackWorkshop: FC = () => {
  const data = useStaticQuery<GetFullStackWorkshopQuery>(graphql`
    query getFullStackWorkshop {
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
    box-shadow: 0 9px 18px rgba(0, 0, 0, 0.25);
  }
`;
