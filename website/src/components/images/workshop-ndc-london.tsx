import { graphql, useStaticQuery } from "gatsby";
import { GatsbyImage } from "gatsby-plugin-image";
import React, { FC } from "react";

import { GetWorkshopNdcLondonImageQuery } from "@/graphql-types";

export const WorkshopNdcLondon: FC = () => {
  const data = useStaticQuery<GetWorkshopNdcLondonImageQuery>(graphql`
    query getWorkshopNdcLondonImage {
      file(
        relativePath: { eq: "workshops/ndc-london.png" }
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
      alt="NDC London"
    />
  );
};
