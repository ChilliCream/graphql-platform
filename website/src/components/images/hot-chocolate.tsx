import { graphql, useStaticQuery } from "gatsby";
import Img from "gatsby-image";
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
          fluid(maxWidth: 1200, pngQuality: 90) {
            ...GatsbyImageSharpFluid
          }
        }
      }
    }
  `);

  return <Img fluid={data.file?.childImageSharp?.fluid as any} />;
};
