import { graphql, useStaticQuery } from "gatsby";
import { GatsbyImage } from "gatsby-plugin-image";
import React, { FC } from "react";
import styled from "styled-components";

import { GetBananaCakePopImageQuery } from "@/graphql-types";

export interface BananaCakePopProps {
  readonly shadow?: boolean;
}

export const BananaCakePop: FC<BananaCakePopProps> = ({ shadow }) => {
  const data = useStaticQuery<GetBananaCakePopImageQuery>(graphql`
    query getBananaCakePopImage {
      file(
        relativePath: { eq: "banana-cake-pop.png" }
        sourceInstanceName: { eq: "images" }
      ) {
        childImageSharp {
          gatsbyImageData(layout: CONSTRAINED, width: 1200, quality: 100)
        }
      }
    }
  `);

  return shadow ? (
    <Container>
      <GatsbyImage
        image={data.file?.childImageSharp?.gatsbyImageData}
        alt="Banana Cake Pop"
      />
    </Container>
  ) : (
    <GatsbyImage
      image={data.file?.childImageSharp?.gatsbyImageData}
      alt="Banana Cake Pop"
    />
  );
};

const Container = styled.div`
  padding: 30px;

  .gatsby-image-wrapper {
    border-radius: var(--border-radius);
    box-shadow: 0 9px 18px rgba(0, 0, 0, 0.25);
  }
`;
