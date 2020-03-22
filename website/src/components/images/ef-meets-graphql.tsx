import { graphql, useStaticQuery } from "gatsby";
import Img from "gatsby-image";
import React, { FunctionComponent } from "react";

export const EFMeetsGraphQL: FunctionComponent = () => {
  const data = useStaticQuery(graphql`
    query getEFMeetsGraphQLImage {
      placeholderImage: file(
        relativePath: { eq: "ef-meets-graphql.png" }
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
