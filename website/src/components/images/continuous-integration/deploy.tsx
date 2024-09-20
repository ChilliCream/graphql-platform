import { graphql, useStaticQuery } from "gatsby";
import { GatsbyImage } from "gatsby-plugin-image";
import React, { FC } from "react";

import { GetContinuousIntegrationDeployImageQuery } from "@/graphql-types";

export const CONTINUOUS_INTEGRATION_DEPLOY_IMAGE_WIDTH = 992;

export const ContinuousIntegrationDeployImage: FC = () => {
  const data = useStaticQuery<GetContinuousIntegrationDeployImageQuery>(graphql`
    query getContinuousIntegrationDeployImage {
      file(
        relativePath: { eq: "continuous-integration/deploy.png" }
        sourceInstanceName: { eq: "images" }
      ) {
        childImageSharp {
          gatsbyImageData(layout: CONSTRAINED, width: 992, quality: 100)
        }
      }
    }
  `);

  return (
    <GatsbyImage
      image={data.file?.childImageSharp?.gatsbyImageData}
      style={{ maxWidth: CONTINUOUS_INTEGRATION_DEPLOY_IMAGE_WIDTH + "px" }}
      alt="Connect Your Ecosystem"
    />
  );
};
