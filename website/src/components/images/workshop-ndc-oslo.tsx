import { graphql, useStaticQuery } from "gatsby";
import { GatsbyImage } from "gatsby-plugin-image";
import React, { FC } from "react";

import { GetWorkshopNdcOsloImageQuery } from "@/graphql-types";

export const WorkshopNdcOsloImage: FC = () => {
  const data = useStaticQuery<GetWorkshopNdcOsloImageQuery>(graphql`
    query getWorkshopNdcOsloImage {
      file(
        relativePath: { eq: "workshops/ndc-oslo.jpg" }
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
      alt="NDC Oslo"
    />
  );
};
