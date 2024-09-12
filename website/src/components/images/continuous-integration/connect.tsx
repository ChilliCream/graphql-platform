import { graphql, useStaticQuery } from "gatsby";
import { GatsbyImage } from "gatsby-plugin-image";
import React, { FC } from "react";

import { GetContinuousIntegrationConnectImageQuery } from "@/graphql-types";

export const CONTINUOUS_INTEGRATION_CONNECT_IMAGE_WIDTH = 869;

export const ContinuousIntegrationConnectImage: FC = () => {
  const data =
    useStaticQuery<GetContinuousIntegrationConnectImageQuery>(graphql`
      query getContinuousIntegrationConnectImage {
        file(
          relativePath: { eq: "continuous-integration/connect.png" }
          sourceInstanceName: { eq: "images" }
        ) {
          childImageSharp {
            gatsbyImageData(layout: CONSTRAINED, width: 869, quality: 100)
          }
        }
      }
    `);

  return (
    <GatsbyImage
      image={data.file?.childImageSharp?.gatsbyImageData}
      style={{ maxWidth: CONTINUOUS_INTEGRATION_CONNECT_IMAGE_WIDTH + "px" }}
      alt="Connect Your Ecosystem"
    />
  );
};
