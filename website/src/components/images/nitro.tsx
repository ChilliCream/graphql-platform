import { graphql, useStaticQuery } from "gatsby";
import { GatsbyImage } from "gatsby-plugin-image";
import React, { FC } from "react";

import { GetNitroAppImageQuery } from "@/graphql-types";

export const NitroAppImage: FC = () => {
  const data = useStaticQuery<GetNitroAppImageQuery>(graphql`
    query getNitroAppImage {
      file(
        relativePath: { eq: "nitro/nitro-app.png" }
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
      alt="Nitro App"
    />
  );
};
