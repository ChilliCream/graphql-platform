import { graphql, useStaticQuery } from "gatsby";
import { GatsbyImage } from "gatsby-plugin-image";
import React, { FC } from "react";

import { GetWorkshopDotNetDaysLasiImageQuery } from "@/graphql-types";

export const WorkshopDotNetDaysLasi: FC = () => {
  const data = useStaticQuery<GetWorkshopDotNetDaysLasiImageQuery>(graphql`
    query getWorkshopDotNetDaysLasiImage {
      file(
        relativePath: { eq: "workshops/dotnetdays-lasi.png" }
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
      alt="dotnetdays lasi"
    />
  );
};
