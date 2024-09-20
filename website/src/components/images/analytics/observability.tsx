import { graphql, useStaticQuery } from "gatsby";
import { GatsbyImage } from "gatsby-plugin-image";
import React, { FC } from "react";

import { GetAnalyticsObservabilityImageQuery } from "@/graphql-types";

export const ANALYTICS_OBSERVABILITY_IMAGE_WIDTH = 1200;

export const AnalyticsObservabilityImage: FC = () => {
  const data = useStaticQuery<GetAnalyticsObservabilityImageQuery>(graphql`
    query getAnalyticsObservabilityImage {
      file(
        relativePath: { eq: "analytics/observability.png" }
        sourceInstanceName: { eq: "images" }
      ) {
        childImageSharp {
          gatsbyImageData(layout: CONSTRAINED, width: 1200, quality: 100)
        }
      }
    }
  `);

  return (
    <GatsbyImage
      image={data.file?.childImageSharp?.gatsbyImageData}
      style={{ maxWidth: ANALYTICS_OBSERVABILITY_IMAGE_WIDTH + "px" }}
      alt="Observability in Focus"
    />
  );
};
