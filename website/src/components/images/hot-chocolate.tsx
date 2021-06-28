import { graphql, useStaticQuery } from "gatsby";
import { GatsbyImage } from "gatsby-plugin-image";
import React, { FunctionComponent } from "react";
import { GetHotChocolateImageQuery } from "../../../graphql-types";

export const HotChocolate: FunctionComponent = () => {
  const data = useStaticQuery<GetHotChocolateImageQuery>(graphql`
    query getHotChocolateImage {
      file(
        relativePath: { eq: "hot-chocolate-console.png" }
        sourceInstanceName: { eq: "images" }
      ) {
        childImageSharp {
          gatsbyImageData(
            layout: CONSTRAINED
            width: 1200
            pngOptions: { quality: 90 }
          )
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
