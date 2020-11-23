import { graphql, useStaticQuery } from "gatsby";
import Img from "gatsby-image";
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
          fluid(maxWidth: 1200, pngQuality: 90) {
            ...GatsbyImageSharpFluid
          }
        }
      }
    }
  `);

  return <Img fluid={data.file?.childImageSharp?.fluid as any} />;
};
