import { graphql, useStaticQuery } from "gatsby";
import { GatsbyImage } from "gatsby-plugin-image";
import React, { FC } from "react";

import { GetEcosystemBannerImageQuery } from "@/graphql-types";

export const ECOSYSTEM_BANNER_IMAGE_WIDTH = 600;

export const EcosystemBannerImage: FC = () => {
  const data = useStaticQuery<GetEcosystemBannerImageQuery>(graphql`
    query getEcosystemBannerImage {
      file(
        relativePath: { eq: "ecosystem/banner.png" }
        sourceInstanceName: { eq: "images" }
      ) {
        childImageSharp {
          gatsbyImageData(layout: CONSTRAINED, width: 600, quality: 100)
        }
      }
    }
  `);

  return (
    <GatsbyImage
      image={data.file?.childImageSharp?.gatsbyImageData}
      style={{ maxWidth: ECOSYSTEM_BANNER_IMAGE_WIDTH + "px" }}
      alt="An Ecosystem You Love"
    />
  );
};
