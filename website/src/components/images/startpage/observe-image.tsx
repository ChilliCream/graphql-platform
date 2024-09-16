import { graphql, useStaticQuery } from "gatsby";
import { GatsbyImage } from "gatsby-plugin-image";
import React, { FC } from "react";

import { GetObserveImageQuery } from "@/graphql-types";

export const OBSERVE_IMAGE_WIDTH = 670;

export const ObserveImage: FC = () => {
  const data = useStaticQuery<GetObserveImageQuery>(graphql`
    query getObserveImage {
      file(
        relativePath: { eq: "startpage/observe.png" }
        sourceInstanceName: { eq: "images" }
      ) {
        childImageSharp {
          gatsbyImageData(layout: CONSTRAINED, width: 670, quality: 100)
        }
      }
    }
  `);

  return (
    <GatsbyImage
      image={data.file?.childImageSharp?.gatsbyImageData}
      style={{ maxWidth: OBSERVE_IMAGE_WIDTH + "px" }}
      alt="Observe"
    />
  );
};
