import { graphql, useStaticQuery } from "gatsby";
import { GatsbyImage } from "gatsby-plugin-image";
import React, { FC } from "react";

import { GetWorkshopDotNetDaysIasiImageQuery } from "@/graphql-types";

export const WorkshopDotNetDaysIasi: FC = () => {
  const data = useStaticQuery<GetWorkshopDotNetDaysIasiImageQuery>(graphql`
    query getWorkshopDotNetDaysLasiImage {
      file(
        relativePath: { eq: "workshops/dotnetdays-iasi.png" }
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
      alt="dotnetdays Iasi"
    />
  );
};
