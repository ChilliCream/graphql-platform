import { graphql, useStaticQuery } from "gatsby";
import { GatsbyImage } from "gatsby-plugin-image";
import React, { FC } from "react";

import { GetContinuousIntegrationTrackImageQuery } from "@/graphql-types";

export const CONTINUOUS_INTEGRATION_TRACK_IMAGE_WIDTH = 1043;

export const ContinuousIntegrationTrackImage: FC = () => {
  const data = useStaticQuery<GetContinuousIntegrationTrackImageQuery>(graphql`
    query getContinuousIntegrationTrackImage {
      file(
        relativePath: { eq: "continuous-integration/track.png" }
        sourceInstanceName: { eq: "images" }
      ) {
        childImageSharp {
          gatsbyImageData(layout: CONSTRAINED, width: 1043, quality: 100)
        }
      }
    }
  `);

  return (
    <GatsbyImage
      image={data.file?.childImageSharp?.gatsbyImageData}
      style={{ maxWidth: CONTINUOUS_INTEGRATION_TRACK_IMAGE_WIDTH + "px" }}
      alt="Connect Your Ecosystem"
    />
  );
};
