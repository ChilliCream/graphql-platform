import { graphql, useStaticQuery } from "gatsby";
import { GatsbyImage } from "gatsby-plugin-image";
import React, { FC } from "react";

import { GetAnalyticsOverviewImageQuery } from "@/graphql-types";

export const ANALYTICS_OVERVIEW_IMAGE_WIDTH = 1000;

export const AnalyticsOverviewImage: FC = () => {
  const data = useStaticQuery<GetAnalyticsOverviewImageQuery>(graphql`
    query getAnalyticsOverviewImage {
      file(
        relativePath: { eq: "analytics/overview.png" }
        sourceInstanceName: { eq: "images" }
      ) {
        childImageSharp {
          gatsbyImageData(layout: CONSTRAINED, width: 1000, quality: 100)
        }
      }
    }
  `);

  return (
    <GatsbyImage
      image={data.file?.childImageSharp?.gatsbyImageData}
      style={{ maxWidth: ANALYTICS_OVERVIEW_IMAGE_WIDTH + "px" }}
      alt="Overview from Every Angle"
    />
  );
};
