import { graphql, useStaticQuery } from "gatsby";
import { GatsbyImage } from "gatsby-plugin-image";
import React, { FC } from "react";

import { GetCollaborateImageQuery } from "@/graphql-types";

export const COLLABORATE_IMAGE_WIDTH = 655;

export const CollaborateImage: FC = () => {
  const data = useStaticQuery<GetCollaborateImageQuery>(graphql`
    query getCollaborateImage {
      file(
        relativePath: { eq: "startpage/collaborate.png" }
        sourceInstanceName: { eq: "images" }
      ) {
        childImageSharp {
          gatsbyImageData(layout: CONSTRAINED, width: 655, quality: 100)
        }
      }
    }
  `);

  return (
    <GatsbyImage
      image={data.file?.childImageSharp?.gatsbyImageData}
      style={{ maxWidth: COLLABORATE_IMAGE_WIDTH + "px" }}
      alt="Observe"
    />
  );
};
