import { graphql, useStaticQuery } from "gatsby";
import { GatsbyImage } from "gatsby-plugin-image";
import React, { FC } from "react";

import { GetNitroBannerImageQuery } from "@/graphql-types";

export const NITRO_BANNER_IMAGE_WIDTH = 1200;

export const NitroBannerImage: FC = () => {
  const data = useStaticQuery<GetNitroBannerImageQuery>(graphql`
    query getNitroBannerImage {
      file(
        relativePath: { eq: "nitro/nitro-banner.png" }
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
      style={{ maxWidth: NITRO_BANNER_IMAGE_WIDTH + "px" }}
      alt="Nitro"
    />
  );
};
