import { graphql, useStaticQuery } from "gatsby";
import { GatsbyImage } from "gatsby-plugin-image";
import React, { FC } from "react";

import { GetHotChocolateImageQuery } from "@/graphql-types";

export const HotChocolate: FC = () => {
  const data = useStaticQuery<GetHotChocolateImageQuery>(graphql`
    query getHotChocolateImage {
      file(
        relativePath: { eq: "hot-chocolate-console.png" }
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
      alt="Hot Chocolate GraphQL server"
    />
  );
};
