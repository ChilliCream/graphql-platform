import { graphql, useStaticQuery } from "gatsby";
import { GatsbyImage } from "gatsby-plugin-image";
import React, { FC } from "react";

import { GetNitroImageQuery } from "@/graphql-types";

export const NitroImage: FC = () => {
  const data = useStaticQuery<GetNitroImageQuery>(graphql`
    query getNitroImage {
      file(
        relativePath: { eq: "banana-cake-pop.png" }
        sourceInstanceName: { eq: "images" }
      ) {
        childImageSharp {
          gatsbyImageData(layout: CONSTRAINED, width: 1200, quality: 100)
        }
      }
    }
  `);

  return (
    <GatsbyImage
      image={data.file?.childImageSharp?.gatsbyImageData}
      alt="Nitro"
    />
  );
};
