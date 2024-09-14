import { graphql, useStaticQuery } from "gatsby";
import { GatsbyImage } from "gatsby-plugin-image";
import React, { FC } from "react";

import { GetAnalyticsBannerImageQuery } from "@/graphql-types";

export const ANALYTICS_BANNER_IMAGE_WIDTH = 1200;

export const AnalyticsBannerImage: FC = () => {
  const data = useStaticQuery<GetAnalyticsBannerImageQuery>(graphql`
    query getAnalyticsBannerImage {
      file(
        relativePath: { eq: "analytics/banner.png" }
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
      style={{ maxWidth: ANALYTICS_BANNER_IMAGE_WIDTH + "px" }}
      alt="Instant Insights. Enhanced Performance."
    />
  );
};
