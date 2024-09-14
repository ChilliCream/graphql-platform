import { graphql, useStaticQuery } from "gatsby";
import { GatsbyImage } from "gatsby-plugin-image";
import React, { FC } from "react";

import { GetEcosystemContinuousEvolutionImageQuery } from "@/graphql-types";

export const ECOSYSTEM_CONTINUOUS_EVOLUTION_IMAGE_WIDTH = 1198;

export const EcosystemContinuousEvolutionImage: FC = () => {
  const data =
    useStaticQuery<GetEcosystemContinuousEvolutionImageQuery>(graphql`
      query getEcosystemContinuousEvolutionImage {
        file(
          relativePath: { eq: "ecosystem/continuous-evolution.png" }
          sourceInstanceName: { eq: "images" }
        ) {
          childImageSharp {
            gatsbyImageData(layout: CONSTRAINED, width: 1198, quality: 100)
          }
        }
      }
    `);

  return (
    <GatsbyImage
      image={data.file?.childImageSharp?.gatsbyImageData}
      style={{ maxWidth: ECOSYSTEM_CONTINUOUS_EVOLUTION_IMAGE_WIDTH + "px" }}
      alt="An Ecosystem You Love"
    />
  );
};
