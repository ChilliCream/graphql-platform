import { graphql, useStaticQuery } from "gatsby";
import { GatsbyImage } from "gatsby-plugin-image";
import React, { FC } from "react";

import { GetWorkshopNdcCopenhagenImageQuery } from "@/graphql-types";

export const WorkshopNdcCopenhagenImage: FC = () => {
  const data = useStaticQuery<GetWorkshopNdcCopenhagenImageQuery>(graphql`
    query getWorkshopNdcCopenhagenImage {
      file(
        relativePath: { eq: "workshops/ndc-copenhagen.jpg" }
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
      alt="NDC Copenhagen"
    />
  );
};
