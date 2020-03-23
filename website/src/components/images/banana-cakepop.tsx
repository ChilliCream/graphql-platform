import { graphql, useStaticQuery } from "gatsby";
import Img from "gatsby-image";
import React, { FunctionComponent } from "react";

const BananaCakepop: FunctionComponent = () => {
  const data = useStaticQuery(graphql`
    query getBananaCakepopImage {
      placeholderImage: file(
        relativePath: { eq: "banana-cakepop.png" }
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

export default BananaCakepop;
