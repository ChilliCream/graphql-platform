import { graphql, useStaticQuery } from "gatsby";
import { GatsbyImage } from "gatsby-plugin-image";
import React, { FC } from "react";

import { GetEvolveImageQuery } from "@/graphql-types";

export const EVOLVE_IMAGE_WIDTH = 524;

export const EvolveImage: FC = () => {
  const data = useStaticQuery<GetEvolveImageQuery>(graphql`
    query getEvolveImage {
      file(
        relativePath: { eq: "startpage/evolve.png" }
        sourceInstanceName: { eq: "images" }
      ) {
        childImageSharp {
          gatsbyImageData(layout: CONSTRAINED, width: 524, quality: 100)
        }
      }
    }
  `);

  return (
    <GatsbyImage
      image={data.file?.childImageSharp?.gatsbyImageData}
      style={{ maxWidth: EVOLVE_IMAGE_WIDTH + "px" }}
      alt="Observe"
    />
  );
};
