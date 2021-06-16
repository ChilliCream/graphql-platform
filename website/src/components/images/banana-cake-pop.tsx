import { graphql, useStaticQuery } from "gatsby";
import { GatsbyImage } from "gatsby-plugin-image";
import React, { FunctionComponent } from "react";
import { GetBananaCakePopImageQuery } from "../../../graphql-types";

export const BananaCakePop: FunctionComponent = () => {
  const data = useStaticQuery<GetBananaCakePopImageQuery>(graphql`
    query getBananaCakePopImage {
      file(
        relativePath: { eq: "banana-cake-pop.png" }
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
      alt="Banana Cake Pop"
    />
  );
};
