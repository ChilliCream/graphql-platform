import { graphql, useStaticQuery } from "gatsby";
import { GatsbyImage } from "gatsby-plugin-image";
import React, { FC } from "react";

import { GetWorkshopNdcMinnesotaImageQuery } from "@/graphql-types";

export const WorkshopNdcMinnesota: FC = () => {
  const data = useStaticQuery<GetWorkshopNdcMinnesotaImageQuery>(graphql`
    query getWorkshopNdcMinnesotaImage {
      file(
        relativePath: { eq: "workshops/ndc-minnesota.jpg" }
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
      alt="NDC Minnesota"
    />
  );
};
