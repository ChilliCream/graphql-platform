import { graphql, useStaticQuery } from "gatsby";
import { GatsbyImage } from "gatsby-plugin-image";
import React, { FC } from "react";

import { GetShipWithConfidenceImageQuery } from "@/graphql-types";

export const SHIP_WITH_CONFIDENCE_IMAGE_WIDTH = 650;

export const ShipWithConfidenceImage: FC = () => {
  const data = useStaticQuery<GetShipWithConfidenceImageQuery>(graphql`
    query getShipWithConfidenceImage {
      file(
        relativePath: { eq: "startpage/ship-with-confidence.png" }
        sourceInstanceName: { eq: "images" }
      ) {
        childImageSharp {
          gatsbyImageData(layout: CONSTRAINED, width: 650, quality: 100)
        }
      }
    }
  `);

  return (
    <GatsbyImage
      image={data.file?.childImageSharp?.gatsbyImageData}
      style={{ maxWidth: SHIP_WITH_CONFIDENCE_IMAGE_WIDTH + "px" }}
      alt="Ship With Confidence"
    />
  );
};
