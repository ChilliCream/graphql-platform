import { graphql, useStaticQuery } from "gatsby";
import { GatsbyImage } from "gatsby-plugin-image";
import React, { FC } from "react";

import { GetMoveFasterImageQuery } from "@/graphql-types";

export const MOVE_FASTER_IMAGE_WIDTH = 658;

export const MoveFasterImage: FC = () => {
  const data = useStaticQuery<GetMoveFasterImageQuery>(graphql`
    query getMoveFasterImage {
      file(
        relativePath: { eq: "startpage/move-faster.png" }
        sourceInstanceName: { eq: "images" }
      ) {
        childImageSharp {
          gatsbyImageData(layout: CONSTRAINED, width: 658, quality: 100)
        }
      }
    }
  `);

  return (
    <GatsbyImage
      image={data.file?.childImageSharp?.gatsbyImageData}
      style={{ maxWidth: MOVE_FASTER_IMAGE_WIDTH + "px" }}
      alt="Move Faster"
    />
  );
};
