import { graphql, useStaticQuery } from "gatsby";
import { GatsbyImage } from "gatsby-plugin-image";
import React, { FC } from "react";

import { GetWorkshopOnlineImageQuery } from "@/graphql-types";

export const WorkshopOnlineImage: FC = () => {
  const data = useStaticQuery<GetWorkshopOnlineImageQuery>(graphql`
    query getWorkshopOnlineImage {
      file(
        relativePath: { eq: "workshops/online.jpg" }
        sourceInstanceName: { eq: "images" }
      ) {
        childImageSharp {
          gatsbyImageData(layout: CONSTRAINED, width: 400, quality: 100)
        }
      }
    }
  `);

  return (
    <GatsbyImage
      image={data.file?.childImageSharp?.gatsbyImageData}
      alt="Online"
    />
  );
};
