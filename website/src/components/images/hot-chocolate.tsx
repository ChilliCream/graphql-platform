import { graphql, useStaticQuery } from "gatsby";
import Img from "gatsby-image";
import React, { FunctionComponent } from "react";

export const HotChocolate: FunctionComponent = () => {
  const data = useStaticQuery(graphql`
    query getHotChocolateImage {
      placeholderImage: file(
        relativePath: { eq: "hot-chocolate-console.png" }
        sourceInstanceName: { eq: "images" }
      ) {
        childImageSharp {
          fluid(maxWidth: 1200) {
            ...GatsbyImageSharpFluid
          }
        }
      }
    }
  `);

  return <Img fluid={data.placeholderImage.childImageSharp.fluid} />;
};
