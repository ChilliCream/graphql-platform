import { graphql, useStaticQuery } from "gatsby";
import { GatsbyImage } from "gatsby-plugin-image";
import React, { FC } from "react";

import { GetAnalyticsInsightsImageQuery } from "@/graphql-types";

export const ANALYTICS_INSIGHTS_IMAGE_WIDTH = 1200;

export const AnalyticsInsightsImage: FC = () => {
  const data = useStaticQuery<GetAnalyticsInsightsImageQuery>(graphql`
    query getAnalyticsInsightsImage {
      file(
        relativePath: { eq: "analytics/insights.png" }
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
      style={{ maxWidth: ANALYTICS_INSIGHTS_IMAGE_WIDTH + "px" }}
      alt="Trace, Detail, Insight."
    />
  );
};
