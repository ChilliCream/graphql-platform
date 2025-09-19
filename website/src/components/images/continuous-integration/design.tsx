import { graphql, useStaticQuery } from "gatsby";
import { GatsbyImage } from "gatsby-plugin-image";
import React, { FC } from "react";

import { GetContinuousIntegrationDesignImageQuery } from "@/graphql-types";

export const CONTINUOUS_INTEGRATION_DESIGN_IMAGE_WIDTH = 1043;

export const ContinuousIntegrationDesignImage: FC = () => {
  const data = useStaticQuery<GetContinuousIntegrationDesignImageQuery>(graphql`
    query getContinuousIntegrationDesignImage {
      file(
        relativePath: { eq: "continuous-integration/design.png" }
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
      style={{ maxWidth: CONTINUOUS_INTEGRATION_DESIGN_IMAGE_WIDTH + "px" }}
      alt="Connect Your Ecosystem"
    />
  );
};
